// using'и оставлены для контекста, некоторые могут быть не нужны после рефакторинга
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// Убраны зависимости от System.Windows, System.Windows.Threading
// Код теперь не зависит от UI-фреймворка

namespace vMixAPI
{

    /// <summary>
    /// Представляет действие со слабой ссылкой на целевой объект,
    /// чтобы избежать утечек памяти, когда подписчик события должен быть собран сборщиком мусора.
    /// </summary>
    public class WeakAction
    {
        private readonly WeakReference _targetRef;
        private readonly MethodInfo _method;
        private readonly bool _isStatic;

        public WeakAction(Action<string, Exception> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            if (action.Target != null)
            {
                _targetRef = new WeakReference(action.Target);
            }
            _method = action.Method;
            _isStatic = action.Target == null;
        }

        /// <summary>
        /// Проверяет, жив ли целевой объект делегата.
        /// </summary>
        public bool IsAlive => _isStatic || (_targetRef?.IsAlive == true);

        /// <summary>
        /// Вызывает действие, если целевой объект все еще доступен.
        /// </summary>
        public void Invoke(string response, Exception error)
        {
            object target = null;
            if (!_isStatic)
            {
                if (_targetRef?.IsAlive != true) return;
                target = _targetRef.Target;
            }

            // Проверка target != null для нестатических методов важна на случай,
            // если объект был уничтожен между проверкой IsAlive и вызовом.
            if (_isStatic || target != null)
            {
                _method.Invoke(target, new object[] { response, error });
            }
        }
    }

    /// <summary>
    /// Группирует и кэширует HTTP-запросы к vMix API.
    /// Использует адаптивное время кэширования: если сервер отвечает медленно, время жизни кэша увеличивается,
    /// чтобы снизить нагрузку на сервер.
    /// </summary>
    public class vMixHttpBatcher : IDisposable
    {
        // --- Поля ---
        private readonly HttpClient _httpClient;
        private readonly bool _isClientOwned;

        // --- Поля для АДАПТИВНОГО кэширования ---
        private const int MinCacheDurationMs = 50;
        private const int DefaultCacheDurationMs = 100;
        private const int MaxCacheDurationMs = 1000;

        /// <summary>
        /// Текущее адаптивное время жизни кэша. volatile для безопасного чтения из разных потоков.
        /// </summary>
        private volatile int _adaptiveCacheDurationMs;

        private readonly ConcurrentDictionary<string, (string Response, long Timestamp)> _cache = new ConcurrentDictionary<string, (string, long)>();

        // --- Поля для батчинга (в текущей реализации не используются, но сохранены) ---
        private readonly ConcurrentQueue<(Uri uri, TaskCompletionSource<string> tcs, WeakAction callback, CancellationTokenSource token)> _requestQueue = new ConcurrentQueue<(Uri, TaskCompletionSource<string>, WeakAction, CancellationTokenSource)>();
        private static readonly ConcurrentDictionary<vMixHttpBatcher, byte> _activeInstances = new ConcurrentDictionary<vMixHttpBatcher, byte>();
        private readonly SemaphoreSlim _processingSemaphore = new SemaphoreSlim(1, 1);

        private volatile bool _disposed;

        public event Action<string, Exception, Uri> OnDownloadCompleted;

        // --- Конструктор ---
        public vMixHttpBatcher(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _isClientOwned = false;
            _adaptiveCacheDurationMs = DefaultCacheDurationMs; // Устанавливаем начальное значение
            _activeInstances.TryAdd(this, 0);
        }

        // --- Публичные методы ---

        /// <summary>
        /// Асинхронно запрашивает строковый ответ по указанному адресу.
        /// Использует адаптивный кэш для запросов без параметров.
        /// </summary>
        public Task<string> GetStringAsync(Uri address, WeakAction callback = null, bool post = false)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(vMixHttpBatcher));

            bool useCache = string.IsNullOrEmpty(address.Query) || address.Query == "?";
            if (useCache)
            {
                string cacheKey = address.GetLeftPart(UriPartial.Path);

                if (_cache.TryGetValue(cacheKey, out var cachedItem))
                {
                    // Используем _adaptiveCacheDurationMs для проверки свежести кэша
                    if ((Environment.TickCount - cachedItem.Timestamp) < _adaptiveCacheDurationMs)
                    {
                        callback?.Invoke(cachedItem.Response, null);
                        OnDownloadCompleted?.Invoke(cachedItem.Response, null, address);
                        return Task.FromResult(cachedItem.Response);
                    }
                }
            }

            return ExecuteAndCacheRequestAsync(address, callback, useCache, post);
        }

        // --- Приватные методы ---

        /// <summary>
        /// Выполняет HTTP-запрос, измеряет время ответа, обновляет адаптивный кэш и возвращает результат.
        /// </summary>
        private async Task<string> ExecuteAndCacheRequestAsync(Uri address, WeakAction callback, bool shouldCache, bool post = false)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                string response = null;
                if (!post)
                    response = await _httpClient.GetStringAsync(address).ConfigureAwait(false);
                else
                {
                    var hresponse = await _httpClient.PostAsync(address, null).ConfigureAwait(false);
                    response = await hresponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                }

                    // Запрос успешен, останавливаем таймер и обновляем адаптивное время
                    stopwatch.Stop();
                UpdateAdaptiveCacheDuration(stopwatch.ElapsedMilliseconds);

                if (shouldCache)
                {
                    string cacheKey = address.GetLeftPart(UriPartial.Path);
                    var newItem = (Response: response, Timestamp: Environment.TickCount);
                    _cache.AddOrUpdate(cacheKey, newItem, (key, oldItem) => newItem);
                }

                callback?.Invoke(response, null);
                OnDownloadCompleted?.Invoke(response, null, address);
                return response;
            }
            catch (Exception ex)
            {
                // Не обновляем время кэша при ошибке, чтобы временные сбои сети
                // не приводили к неоправданно долгому кэшированию в будущем.
                stopwatch.Stop();
                callback?.Invoke(null, ex);
                OnDownloadCompleted?.Invoke(null, ex, address);
                throw;
            }
        }

        /// <summary>
        /// Потокобезопасно обновляет время жизни кэша на основе последнего времени ответа сервера.
        /// </summary>
        /// <param name="measuredLatency">Время ответа сервера в миллисекундах.</param>
        private void UpdateAdaptiveCacheDuration(long measuredLatency)
        {
            // Устанавливаем новое время кэша равным времени ответа,
            // но ограничиваем его в пределах Min и Max.
            int newDuration = (int)Math.Max(MinCacheDurationMs, Math.Min(MaxCacheDurationMs, measuredLatency));

            // Атомарно обновляем значение, чтобы избежать гонки потоков.
            Interlocked.Exchange(ref _adaptiveCacheDurationMs, newDuration);
        }

        public bool IsProcessing() => _processingSemaphore.CurrentCount == 0 || !_requestQueue.IsEmpty;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            GC.SuppressFinalize(this);

            _activeInstances.TryRemove(this, out _);
            _cache.Clear();

            while (_requestQueue.TryDequeue(out var item))
            {
                item.tcs.TrySetCanceled();
                item.token?.Dispose();
            }

            if (_isClientOwned)
            {
                _httpClient?.Dispose();
            }

            _processingSemaphore?.Dispose();
            OnDownloadCompleted = null;
        }
    }


    /// <summary>
    /// Статический менеджер для управления экземплярами vMixHttpBatcher.
    /// Предоставляет общий HttpClient для всех запросов для повышения эффективности.
    /// </summary>
    public static class APIRequestManagerV2
    {
        // Используем один статический HttpClient для всего приложения. Это лучшая практика.
        private static readonly HttpClient _sharedClient = new HttpClient
        {
            // Таймаут должен быть достаточно большим для обработки "зависших" запросов
            Timeout = TimeSpan.FromSeconds(20)
        };

        private static readonly ConcurrentDictionary<string, (DateTime lastAccess, vMixHttpBatcher batcher)> _batchers = new ConcurrentDictionary<string, (DateTime lastAccess, vMixHttpBatcher batcher)>();
        private static readonly TimeSpan _inactiveTimeout = TimeSpan.FromMinutes(1);
        private static Timer _cleanupTimer;

        static APIRequestManagerV2()
        {
            // Запускаем таймер для периодической очистки неактивных батчеров
            _cleanupTimer = new Timer(_ => CleanupInactiveBatchers(), null, _inactiveTimeout, _inactiveTimeout);
        }

        /// <summary>
        /// Получает ответ от vMix API. Управляет созданием и переиспользованием обработчиков запросов.
        /// </summary>
        /// <param name="apiUrl">Базовый URL API vMix (например, "http://127.0.0.1:8088/api")</param>
        /// <param name="callback">Необязательный callback для получения результата.</param>
        /// <returns>Задача, представляющая XML-ответ от vMix.</returns>
        public static Task<string> GetApiResponseAsync(string apiUrl, WeakAction callback = null, string auth = null, bool post = false)
        {
            if (auth != null)
            {

                _sharedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(auth)));
            }

            string key = apiUrl.EndsWith("/api") || apiUrl.EndsWith("/api?") ? "state" : "function";

            // Используем GetOrAdd для атомарного создания или получения батчера
            var batcherEntry = _batchers.GetOrAdd(key, url =>
                (DateTime.UtcNow, new vMixHttpBatcher(_sharedClient))
            );

            // Обновляем время последнего доступа
            batcherEntry.lastAccess = DateTime.UtcNow;
            _batchers[key] = batcherEntry;

            // Упрощено: напрямую вызываем GetStringAsync и возвращаем его Task
            return batcherEntry.batcher.GetStringAsync(new Uri(apiUrl), callback, post);
        }

        // Методы для подписки/отписки на события конкретного батчера
        public static void RegisterCompletionHandler(string apiUrl, Action<string, Exception, Uri> handler)
        {
            if (_batchers.TryGetValue(apiUrl, out var batcherEntry))
            {
                batcherEntry.batcher.OnDownloadCompleted += handler;
            }
        }

        public static void UnregisterCompletionHandler(string apiUrl, Action<string, Exception, Uri> handler)
        {
            if (_batchers.TryGetValue(apiUrl, out var batcherEntry))
            {
                batcherEntry.batcher.OnDownloadCompleted -= handler;
            }
        }

        private static void CleanupInactiveBatchers()
        {
            var now = DateTime.UtcNow;
            var inactiveKeys = _batchers
                .Where(kvp => (now - kvp.Value.lastAccess > _inactiveTimeout) && !kvp.Value.batcher.IsProcessing())
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in inactiveKeys)
            {
                // Дополнительная проверка на случай, если батчер снова стал активным
                if (_batchers.TryGetValue(key, out var entry) && (now - entry.lastAccess > _inactiveTimeout))
                {
                    if (_batchers.TryRemove(key, out var removedEntry))
                    {
                        removedEntry.batcher.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Принудительно освобождает все ресурсы, используемые менеджером.
        /// Вызывать при завершении работы приложения.
        /// </summary>
        public static void Cleanup()
        {
            _cleanupTimer?.Dispose();
            _cleanupTimer = null;

            foreach (var batcherEntry in _batchers.Values)
            {
                batcherEntry.batcher.Dispose();
            }
            _batchers.Clear();

            _sharedClient?.Dispose();
        }
    }

}