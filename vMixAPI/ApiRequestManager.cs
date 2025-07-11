// using'и оставлены для контекста, некоторые могут быть не нужны после рефакторинга
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
    /// Предоставляет локальный пул для интернирования строк ответов API.
    /// Это позволяет хранить только один экземпляр каждой уникальной XML-строки в памяти,
    /// значительно сокращая ее потребление.
    /// </summary>
    /// <summary>
    /// Предоставляет локальный пул для интернирования строк ответов API
    /// с механизмом очистки давно неиспользуемых записей (LRU Cache).
    /// Это необходимо, если ответы API содержат уникальные данные при каждом запросе.
    /// </summary>
    public static class EvictableXmlResponseInterner
    {
        // Теперь значение хранит не только строку, но и время последнего доступа
        private static readonly ConcurrentDictionary<string, (string value, DateTime lastAccess)> _pool
            = new ConcurrentDictionary<string, (string, DateTime)>();

        private static readonly Timer _cleanupTimer;
        private static readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(2); // Очищать каждые 5 минут
        private static readonly TimeSpan _entryLifetime = TimeSpan.FromMinutes(4); // Хранить запись 10 минут без использования
        private const int MaxPoolSize = 2500; // Максимальный размер пула, чтобы избежать переполнения

        static EvictableXmlResponseInterner()
        {
            // Запускаем таймер, который будет периодически вызывать метод очистки
            _cleanupTimer = new Timer(_ => Cleanup(), null, _cleanupInterval, _cleanupInterval);
        }

        /// <summary>
        /// Возвращает каноническое представление для указанной строки, обновляя время ее последнего использования.
        /// </summary>
        public static string Intern(string response)
        {
            if (string.IsNullOrEmpty(response))
            {
                return response;
            }

            // При каждом доступе обновляем время.
            // AddOrUpdate - потокобезопасная операция.
            var entry = _pool.AddOrUpdate(
                response,
                key => (key, DateTime.UtcNow), // Фабрика для добавления нового элемента
                (key, existing) => (existing.value, DateTime.UtcNow) // Фабрика для обновления существующего
            );

            return entry.value;
        }

        private static void Cleanup()
        {
            try
            {
                var now = DateTime.UtcNow;

                // Находим ключи устаревших записей (давно не использовались)
                var keysToRemove = _pool
                    .Where(pair => (now - pair.Value.lastAccess) > _entryLifetime)
                    .Select(pair => pair.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _pool.TryRemove(key, out _);
                }

                // Если пул все еще слишком большой, принудительно удаляем самые старые записи,
                // даже если они использовались недавно. Это защита от переполнения.
                if (_pool.Count > MaxPoolSize)
                {
                    var oldestKeys = _pool
                        .OrderBy(p => p.Value.lastAccess)
                        .Take(_pool.Count - MaxPoolSize)
                        .Select(p => p.Key)
                        .ToList();

                    foreach (var key in oldestKeys)
                    {
                        _pool.TryRemove(key, out _);
                    }
                }
            }
            catch
            {
                // Подавляем любые исключения внутри таймера, чтобы он не "упал".
            }
        }

        /// <summary>
        /// Останавливает таймер очистки. Следует вызывать при завершении работы приложения.
        /// </summary>
        public static void StopCleanup()
        {
            _cleanupTimer?.Dispose();
        }
    }

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
    /// Группирует и выполняет HTTP-запросы к vMix API, чтобы снизить нагрузку на сервер.
    /// Использует общие таймеры для всех экземпляров для экономии ресурсов.
    /// **ИЗМЕНЕНИЕ**: Таймер очистки кэша теперь является динамическим и привязан к экземпляру.
    /// </summary>
    public class vMixHttpBatcher : IDisposable
    {
        // --- Поля ---
        private readonly HttpClient _httpClient;
        private readonly bool _isClientOwned;

        private readonly ConcurrentDictionary<string, (string response, Exception error, DateTime timestamp)> _responseCache = new ConcurrentDictionary<string, (string, Exception, DateTime)>();
        private const int MaxCacheSize = 1000;
        private const int MaxQueueSize = 2000;

        private readonly ConcurrentQueue<(Uri uri, TaskCompletionSource<string> tcs, WeakAction callback, CancellationTokenSource token)> _requestQueue = new ConcurrentQueue<(Uri, TaskCompletionSource<string>, WeakAction, CancellationTokenSource)>();

        private static readonly object _timerLock = new object();
        private static Timer _sharedBatchTimer;
        // --- ИЗМЕНЕНИЕ: Таймер очистки кэша убран из статических полей ---
        // private static System.Threading.Timer _sharedClearCacheTimer; 
        private static readonly ConcurrentDictionary<vMixHttpBatcher, byte> _activeInstances = new ConcurrentDictionary<vMixHttpBatcher, byte>();

        private const int BatchIntervalMs = 100;

        // --- ИЗМЕНЕНИЕ: Все "магические числа" для интервалов вынесены в константы ---
        private const int DefaultCacheClearIntervalMs = 500; // Интервал при низкой нагрузке
        private const int MinCacheClearIntervalMs = 50;       // Минимальный интервал (при высокой нагрузке)
        private const int MaxCacheClearIntervalMs = 1000;     // Абсолютный максимальный предел интервала
        private const int LowRpsThreshold = 10;               // Порог RPS для низкой нагрузки
        private const int HighRpsThreshold = 100;              // Порог RPS для высокой нагрузки
        private const int LatencyPenaltyMs = 25;              // Штраф к интервалу
        private const int LatencyThresholdMs = 500;           // Порог задержки для применения штрафа

        private readonly SemaphoreSlim _processingSemaphore = new SemaphoreSlim(1, 1);
        private volatile bool _disposed;

        // --- НОВЫЕ ПОЛЯ для динамической адаптации ---
        private readonly Timer _instanceClearCacheTimer; // Индивидуальный таймер очистки кэша
        private readonly Timer _metricsTimer;             // Таймер для сбора метрик и корректировки
        private int _requestCounter = 0;                  // Счетчик запросов для вычисления RPS
        private readonly ConcurrentQueue<long> _recentLatencies = new ConcurrentQueue<long>(); // Очередь последних задержек (в мс)
        private const int LatencySampleSize = 50;         // Количество замеров задержки для усреднения
        private volatile int _currentCacheClearIntervalMs = DefaultCacheClearIntervalMs;

        public event Action<string, Exception, Uri> OnDownloadCompleted;

        // --- Статический конструктор и управление таймерами ---
        static vMixHttpBatcher()
        {
            InitializeSharedTimers();
        }

        private static void InitializeSharedTimers()
        {
            lock (_timerLock)
            {
                if (_sharedBatchTimer == null)
                {
                    _sharedBatchTimer = new Timer(_ => ProcessAllBatches(), null, BatchIntervalMs, BatchIntervalMs);
                }
                // --- ИЗМЕНЕНИЕ: Логика для _sharedClearCacheTimer удалена ---
            }
        }

        private static void StopSharedTimersIfNoInstances()
        {
            lock (_timerLock)
            {
                if (_activeInstances.IsEmpty)
                {
                    _sharedBatchTimer?.Dispose();
                    _sharedBatchTimer = null;
                    // --- ИЗМЕНЕНИЕ: Логика для _sharedClearCacheTimer удалена ---
                }
            }
        }

        private static void ProcessAllBatches()
        {
            Parallel.ForEach(_activeInstances.Keys, instance =>
            {
                if (!instance._disposed && !instance._requestQueue.IsEmpty)
                {
                    _ = instance.ProcessBatchAsync();
                }
            });
        }

        // --- ИЗМЕНЕНИЕ: Статический метод ClearAllExpiredCaches больше не нужен ---

        // --- Конструктор ---
        public vMixHttpBatcher(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _isClientOwned = false;
            _activeInstances.TryAdd(this, 0);
            InitializeSharedTimers();

            // --- НОВОЕ: Инициализация индивидуальных таймеров ---
            // Таймер очистки кэша, который будет адаптироваться
            _instanceClearCacheTimer = new Timer(_ => ClearExpiredCache(), null, _currentCacheClearIntervalMs, _currentCacheClearIntervalMs);
            // Таймер, который раз в секунду пересчитывает метрики и корректирует интервал
            _metricsTimer = new Timer(_ => AdjustCacheTimer(), null, 1000, 1000);
        }

        // --- Публичные методы ---
        public Task<string> GetStringAsync(Uri address, WeakAction callback = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(vMixHttpBatcher));

            // --- НОВОЕ: Увеличиваем счетчик запросов для расчета RPS ---
            Interlocked.Increment(ref _requestCounter);

            bool isCommand = !string.IsNullOrEmpty(address.Query);

            if (isCommand)
            {
                return ExecuteImmediateAsync(address, callback);
            }

            if (_responseCache.TryGetValue(address.ToString(), out var cached))
            {
                callback?.Invoke(cached.response, cached.error);
                return cached.error != null
                    ? Task.FromException<string>(cached.error)
                    : Task.FromResult(cached.response);
            }

            if (_requestQueue.Count >= MaxQueueSize)
            {
                var ex = new InvalidOperationException("Request queue is full. Please try again later.");
                callback?.Invoke(null, ex);
                return Task.FromException<string>(ex);
            }

            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            cts.Token.Register(() => tcs.TrySetCanceled());

            _requestQueue.Enqueue((address, tcs, callback, cts));

            return tcs.Task;
        }

        // --- Приватные методы ---
        private async Task<string> ExecuteImmediateAsync(Uri address, WeakAction callback)
        {
            // --- НОВОЕ: Измеряем задержку и для немедленных запросов ---
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var response = await _httpClient.GetStringAsync(address).ConfigureAwait(false);
                var internedResponse = EvictableXmlResponseInterner.Intern(response);

                if (internedResponse.Contains("Function completed successfully"))
                {
                    ClearApiCache();
                }

                callback?.Invoke(internedResponse, null);
                OnDownloadCompleted?.Invoke(internedResponse, null, address);
                return internedResponse;
            }
            catch (Exception ex)
            {
                callback?.Invoke(null, ex);
                OnDownloadCompleted?.Invoke(null, ex, address);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                RecordLatency(stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task ProcessBatchAsync()
        {
            if (!await _processingSemaphore.WaitAsync(0)) return;

            try
            {
                if (_requestQueue.IsEmpty) return;

                var requestsToProcess = new Dictionary<Uri, List<(TaskCompletionSource<string> tcs, WeakAction callback, CancellationTokenSource token)>>();
                while (_requestQueue.TryDequeue(out var item))
                {
                    if (item.token.IsCancellationRequested)
                    {
                        item.tcs.TrySetCanceled();
                        item.token.Dispose();
                        continue;
                    }

                    if (!requestsToProcess.TryGetValue(item.uri, out var list))
                    {
                        list = new List<(TaskCompletionSource<string> tcs, WeakAction callback, CancellationTokenSource token)>();
                        requestsToProcess[item.uri] = list;
                    }
                    list.Add((item.tcs, item.callback, item.token));
                }

                var tasks = requestsToProcess.Select(kvp => ProcessSingleRequestAsync(kvp.Key, kvp.Value));
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }

        private async Task ProcessSingleRequestAsync(Uri uri, List<(TaskCompletionSource<string> tcs, WeakAction callback, CancellationTokenSource token)> requests)
        {
            Exception error = null;
            string internedResponse = null;

            // --- НОВОЕ: Измеряем задержку с помощью Stopwatch ---
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var responseString = await _httpClient.GetStringAsync(uri).ConfigureAwait(false);
                internedResponse = EvictableXmlResponseInterner.Intern(responseString);
                AddToCache(uri.ToString(), internedResponse, null);
            }
            catch (Exception ex)
            {
                error = ex;
                AddToCache(uri.ToString(), null, ex);
            }
            finally
            {
                stopwatch.Stop();
                RecordLatency(stopwatch.ElapsedMilliseconds);
            }

            foreach (var (tcs, callback, token) in requests)
            {
                if (error != null) tcs.TrySetException(error);
                else tcs.TrySetResult(internedResponse);

                callback?.Invoke(internedResponse, error);
                token.Dispose();
            }

            OnDownloadCompleted?.Invoke(internedResponse, error, uri);
        }

        // --- НОВЫЕ МЕТОДЫ для адаптивной логики ---

        /// <summary>
        /// Записывает задержку выполнения запроса в очередь для последующего анализа.
        /// </summary>
        private void RecordLatency(long elapsedMilliseconds)
        {
            _recentLatencies.Enqueue(elapsedMilliseconds);
            // Поддерживаем постоянный размер выборки, удаляя самые старые значения
            while (_recentLatencies.Count > LatencySampleSize)
            {
                _recentLatencies.TryDequeue(out _);
            }
        }

        /// <summary>
        /// Вызывается раз в секунду для анализа метрик и корректировки интервала очистки кэша.
        /// </summary>
        private void AdjustCacheTimer()
        {
            if (_disposed) return;

            // 1. Получаем RPS и сбрасываем счетчик
            int rps = Interlocked.Exchange(ref _requestCounter, 0);

            // 2. Рассчитываем среднюю задержку
            double averageLatency = _recentLatencies.IsEmpty ? 0 : _recentLatencies.Average();

            // 3. Вычисляем новый интервал на основе RPS, используя именованные константы
            int newInterval;
            if (rps >= HighRpsThreshold)
            {
                newInterval = MinCacheClearIntervalMs;
            }
            else if (rps <= LowRpsThreshold)
            {
                newInterval = DefaultCacheClearIntervalMs;
            }
            else
            {
                // --- ИЗМЕНЕНИЕ: Линейная интерполяция теперь использует константы ---
                // Нормализуем текущий RPS в диапазоне [0, 1] между порогами
                double rpsRange = HighRpsThreshold - LowRpsThreshold;
                double rpsNormalized = (rps - LowRpsThreshold) / rpsRange;

                // Вычисляем диапазон изменения интервала
                double intervalRange = DefaultCacheClearIntervalMs - MinCacheClearIntervalMs;

                // Применяем нормализованный RPS к диапазону интервала и вычитаем из значения по умолчанию
                newInterval = DefaultCacheClearIntervalMs - (int)(rpsNormalized * intervalRange);
            }

            // 4. Добавляем "штраф" за высокую задержку
            if (averageLatency > LatencyThresholdMs)
            {
                int latencyPenalty = (int)(Math.Floor(averageLatency / LatencyThresholdMs) * LatencyPenaltyMs);
                newInterval += latencyPenalty;
            }

            // 5. Ограничиваем интервал абсолютными пределами
            newInterval = Math.Max(MinCacheClearIntervalMs, Math.Min(MaxCacheClearIntervalMs, newInterval));

            // 6. Если интервал изменился, обновляем таймер
            if (newInterval != _currentCacheClearIntervalMs)
            {
                _currentCacheClearIntervalMs = newInterval;
                _instanceClearCacheTimer.Change(newInterval, newInterval);
            }
        }

        private void AddToCache(string key, string response, Exception error)
        {
            if (_responseCache.Count >= MaxCacheSize)
            {
                var oldestKeys = _responseCache.ToArray()
                                               .OrderBy(kvp => kvp.Value.timestamp)
                                               .Take(MaxCacheSize / 10)
                                               .Select(kvp => kvp.Key);
                foreach (var oldKey in oldestKeys)
                {
                    _responseCache.TryRemove(oldKey, out _);
                }
            }
            _responseCache[key] = (response, error, DateTime.UtcNow);
        }

        public void ClearApiCache()
        {
            var apiKeys = _responseCache.Keys.Where(k => k.EndsWith("/api")).ToList();
            foreach (var key in apiKeys)
            {
                _responseCache.TryRemove(key, out _);
            }
        }

        public bool IsProcessing() => _processingSemaphore.CurrentCount == 0 || !_requestQueue.IsEmpty;

        public void ClearExpiredCache()
        {
            var now = DateTime.UtcNow;
            // Используем текущий динамический интервал для определения устаревших записей
            var expiredKeys = _responseCache.Where(kvp => (now - kvp.Value.timestamp).TotalMilliseconds > _currentCacheClearIntervalMs)
                                            .Select(kvp => kvp.Key)
                                            .ToList();

            foreach (var key in expiredKeys)
            {
                _responseCache.TryRemove(key, out _);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            GC.SuppressFinalize(this);

            _activeInstances.TryRemove(this, out _);
            StopSharedTimersIfNoInstances();

            // --- НОВОЕ: Освобождаем индивидуальные таймеры ---
            _metricsTimer?.Dispose();
            _instanceClearCacheTimer?.Dispose();

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
        public static Task<string> GetApiResponseAsync(string apiUrl, WeakAction callback = null, string auth = null)
        {
            if (auth != null)
            {

                _sharedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(auth)));
            }
            // Используем GetOrAdd для атомарного создания или получения батчера
            var batcherEntry = _batchers.GetOrAdd(apiUrl, url =>
                (DateTime.UtcNow, new vMixHttpBatcher(_sharedClient))
            );

            // Обновляем время последнего доступа
            batcherEntry.lastAccess = DateTime.UtcNow;
            _batchers[apiUrl] = batcherEntry;

            // Упрощено: напрямую вызываем GetStringAsync и возвращаем его Task
            return batcherEntry.batcher.GetStringAsync(new Uri(apiUrl), callback);
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