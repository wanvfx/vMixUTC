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
        private readonly WeakReference<object> _targetRef;
        private readonly MethodInfo _method;
        // Открытый делегат — только для обычных методов экземпляра с точной сигнатурой
        private readonly Action<object, string, Exception> _openDelegate;
        // Статический делегат — для статических методов
        private readonly Action<string, Exception> _staticDelegate;
        // Флаг: использовать MethodInfo.Invoke вместо открытого делегата
        private readonly bool _useReflection;

        public WeakAction(Action<string, Exception> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            if (action.Target == null)
            {
                _staticDelegate = action;
                return;
            }

            _targetRef = new WeakReference<object>(action.Target);
            _method = action.Method;

            if (TryCreateOpenDelegate(_method, out var openDelegate))
            {
                _openDelegate = openDelegate;
                _useReflection = false;
            }
            else
                _useReflection = true;
        }

        public bool IsAlive =>
            _staticDelegate != null ||
            _targetRef?.TryGetTarget(out _) == true;

        public void Invoke(string response, Exception error)
        {
            if (_staticDelegate != null)
            {
                _staticDelegate(response, error);
                return;
            }

            if (_targetRef != null && _targetRef.TryGetTarget(out var target))
            {
                if (_useReflection)
                    _method.Invoke(target, new object[] { response, error });
                else
                    _openDelegate(target, response, error);
            }
        }

        private static bool TryCreateOpenDelegate(
            MethodInfo method,
            out Action<object, string, Exception> result)
        {
            result = null;
            // Анонимные методы/лямбды имеют CompilerGeneratedAttribute
            if (method.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false))
                return false;

            try
            {
                var parameters = method.GetParameters();
                if (parameters.Length == 2
                    && parameters[0].ParameterType == typeof(string)
                    && parameters[1].ParameterType == typeof(Exception))
                {
                    result = (Action<object, string, Exception>)Delegate.CreateDelegate(
                        typeof(Action<object, string, Exception>),
                        null,
                        method,
                        throwOnBindFailure: false);
                }
                return result != null;
            }
            catch
            {
                return false;
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
        private sealed class PendingRequest
        {
            NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

            private readonly TaskCompletionSource<string> _tcs
                = new TaskCompletionSource<string>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

            // Потокобезопасный список callback-ов всех ожидающих
            private volatile WeakAction[] _callbacks = Array.Empty<WeakAction>();
            private readonly object _callbackLock = new object();

            public Task<string> Task => _tcs.Task;

            public void AddCallback(WeakAction callback)
            {
                if (callback == null) return;
                lock (_callbackLock)
                {
                    var current = _callbacks;
                    var next = new WeakAction[current.Length + 1];
                    current.CopyTo(next, 0);
                    next[current.Length] = callback;
                    _callbacks = next; // атомарная замена ссылки
                }
            }

            public void Complete(string result, Uri address,
                Action<string, Exception, Uri> onCompleted)
            {
                foreach (var cb in _callbacks)
                {
                    try { cb.Invoke(result, null); }
                    catch (Exception e) { _logger.Warn(e, "Callback failed"); }
                }
                try { onCompleted?.Invoke(result, null, address); }
                catch (Exception e) { _logger.Warn(e, "OnDownloadCompleted failed"); }
                _tcs.TrySetResult(result);
            }

            public void Fail(Exception ex, Uri address,
                Action<string, Exception, Uri> onCompleted)
            {
                foreach (var cb in _callbacks)
                {
                    try { cb.Invoke(null, ex); }
                    catch (Exception e) { _logger.Warn(e, "Callback failed"); }
                }
                try { onCompleted?.Invoke(null, ex, address); }
                catch (Exception e) { _logger.Warn(e, "OnDownloadCompleted failed"); }
                _tcs.TrySetException(ex);
            }

            public void Cancel()
            {
                _tcs.TrySetCanceled();
            }
        }

        NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        // --- Поля ---
        private readonly HttpClient _httpClient;
        private readonly bool _isClientOwned;

        // --- Поля для АДАПТИВНОГО кэширования ---
        private const int MinCacheDurationMs = 50;
        private const int DefaultCacheDurationMs = 100;
        private const int MaxCacheDurationMs = 1000;
        private int _cacheHitCount = 0;
        private const int PurgeEveryNHits = 100;
        private int _activeRequestCount = 0;
        private readonly CancellationTokenSource _disposeCts = new CancellationTokenSource();

        /// <summary>
        /// Текущее адаптивное время жизни кэша. volatile для безопасного чтения из разных потоков.
        /// </summary>
        private volatile int _adaptiveCacheDurationMs;

        private readonly ConcurrentDictionary<string, (string Response, long Timestamp)> _cache = new ConcurrentDictionary<string, (string, long)>();

        private volatile bool _disposed;

        public event Action<string, Exception, Uri> OnDownloadCompleted;

        // --- Конструктор ---
        public vMixHttpBatcher(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _isClientOwned = false;
            _adaptiveCacheDurationMs = DefaultCacheDurationMs;
        }

        private void PurgeExpiredCacheEntries()
        {
            long now = GetTimestampMs();
            long maxAge = MaxCacheDurationMs * 10L;

            foreach (var key in _cache.Keys.ToList())
            {
                if (_cache.TryGetValue(key, out var item) && (now - item.Timestamp) > maxAge)
                    _cache.TryRemove(key, out _);
            }
        }
        private async Task<string> SendWithAuthAsync(Uri uri, string auth, bool post, CancellationToken ct = default)
        {
            using
                (var request = new HttpRequestMessage(
                post ? HttpMethod.Post : HttpMethod.Get, uri))
            {

                if (auth != null)
                {
                    var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(auth));
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Basic", encoded);
                }

                using (var response = await _httpClient
                    .SendAsync(request, ct)
                    .ConfigureAwait(false))
                {

                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
        }

        private long GetTimestampMs()
        {
            long timestamp = Stopwatch.GetTimestamp();
            long seconds = timestamp / Stopwatch.Frequency;
            long remainder = timestamp % Stopwatch.Frequency;
            return seconds * 1000L + (remainder * 1000L) / Stopwatch.Frequency;
        }

        private async Task RunPendingRequestAsync(Uri address, string cacheKey, PendingRequest pending, bool shouldCache, bool post, string auth, CancellationToken ct)
        {
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                ct, _disposeCts.Token))
            {
                var stopwatch = Stopwatch.StartNew();
                Interlocked.Increment(ref _activeRequestCount);
                try
                {
                    var response = await SendWithAuthAsync(
                        address, auth, post, linkedCts.Token).ConfigureAwait(false);

                    stopwatch.Stop();
                    UpdateAdaptiveCacheDuration(stopwatch.ElapsedMilliseconds);

                    if (shouldCache)
                    {
                        var newItem = (Response: response, Timestamp: GetTimestampMs());
                        _cache.AddOrUpdate(cacheKey, newItem, (_, __) => newItem);
                    }

                    _pendingRequests.TryRemove(cacheKey, out _);
                    pending.Complete(response, address, OnDownloadCompleted);
                }
                catch (OperationCanceledException ex)
                {
                    _pendingRequests.TryRemove(cacheKey, out _);
                    pending.Cancel();
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _pendingRequests.TryRemove(cacheKey, out _);
                    pending.Fail(ex, address, OnDownloadCompleted);
                }
                finally
                {
                    Interlocked.Decrement(ref _activeRequestCount);
                }
            }
        }

        private static async Task<T> WaitWithCallerCancellation<T>(Task<T> task, CancellationToken ct)
        {
            if (!ct.CanBeCanceled || task.IsCompleted)
                return await task.ConfigureAwait(false);

            var cancelTcs = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            using (ct.Register(() => cancelTcs.TrySetResult(true)))
            {
                var completed = await Task.WhenAny(task, cancelTcs.Task).ConfigureAwait(false);
                if (!ReferenceEquals(completed, task))
                    throw new OperationCanceledException(ct);
            }

            return await task.ConfigureAwait(false);
        }

        // Хранит "в-процессе" задачи, чтобы не дублировать запросы
        private readonly ConcurrentDictionary<string, PendingRequest> _pendingRequests
            = new ConcurrentDictionary<string, PendingRequest>();
        /// <summary>
        /// Асинхронно запрашивает строковый ответ по указанному адресу.
        /// Использует адаптивный кэш для запросов без параметров.
        /// </summary>
        public Task<string> GetStringAsync(Uri address, WeakAction callback = null, bool post = false, bool ignoreCache = false, string auth = null, CancellationToken ct = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(vMixHttpBatcher));

            bool useCache = string.IsNullOrEmpty(address.Query) || address.Query == "?";
            if (useCache && !ignoreCache)
            {
                string cacheKey = address.GetLeftPart(UriPartial.Path);

                if (_cache.TryGetValue(cacheKey, out var cachedItem)
                    && (GetTimestampMs() - cachedItem.Timestamp) < _adaptiveCacheDurationMs)
                {
                    _logger.Debug($"Return cached response from: {cacheKey}");
                    callback?.Invoke(cachedItem.Response, null);
                    OnDownloadCompleted?.Invoke(cachedItem.Response, null, address);

                    if (Interlocked.Increment(ref _cacheHitCount) % PurgeEveryNHits == 0)
                        Task.Run(() => PurgeExpiredCacheEntries());

                    return Task.FromResult(cachedItem.Response);
                }

                var pending = new PendingRequest();
                var actual = _pendingRequests.GetOrAdd(cacheKey, pending);

                // Регистрируем callback независимо от того, новый это запрос или нет
                actual.AddCallback(callback);

                if (ReferenceEquals(actual, pending))
                {
                    // Мы создали новый запрос — запускаем его
                    _ = RunPendingRequestAsync(address, cacheKey, actual, useCache, post, auth, ct);
                }

                return WaitWithCallerCancellation(actual.Task, ct);
            }

            return ExecuteAndCacheRequestAsync(address, callback, useCache, post, auth, ct);
        }

        // --- Приватные методы ---
        /// <summary>
        /// Выполняет HTTP-запрос, измеряет время ответа, обновляет адаптивный кэш и возвращает результат.
        /// </summary>
        private async Task<string> ExecuteAndCacheRequestAsync(Uri address, WeakAction callback, bool shouldCache, bool post = false, string auth = null, CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _disposeCts.Token))
                try
                {
                    Interlocked.Increment(ref _activeRequestCount);
                    string response = null;
                    response = await SendWithAuthAsync(address, auth, post, linkedCts.Token).ConfigureAwait(false);

                    // Запрос успешен, останавливаем таймер и обновляем адаптивное время
                    stopwatch.Stop();
                    UpdateAdaptiveCacheDuration(stopwatch.ElapsedMilliseconds);

                    if (shouldCache)
                    {
                        string cacheKey = address.GetLeftPart(UriPartial.Path);
                        var newItem = (Response: response, Timestamp: GetTimestampMs());
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
                finally
                {
                    Interlocked.Decrement(ref _activeRequestCount);
                }
        }

        /// <summary>
        /// Потокобезопасно обновляет время жизни кэша на основе последнего времени ответа сервера.
        /// </summary>
        /// <param name="measuredLatency">Время ответа сервера в миллисекундах.</param>
        private void UpdateAdaptiveCacheDuration(long measuredLatency)
        {
            const double alpha = 0.2;
            int current, newValue;
            do
            {
                current = Volatile.Read(ref _adaptiveCacheDurationMs);
                int smoothed = (int)(alpha * measuredLatency + (1.0 - alpha) * current);
                newValue = Math.Max(MinCacheDurationMs, Math.Min(MaxCacheDurationMs, smoothed));
            }
            while (Interlocked.CompareExchange(
                ref _adaptiveCacheDurationMs, newValue, current) != current);
        }

        public bool IsProcessing() => Volatile.Read(ref _activeRequestCount) > 0;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _disposeCts.Cancel();

            // Отменяем все pending-запросы немедленно
            foreach (var kvp in _pendingRequests)
                kvp.Value.Cancel();
            _pendingRequests.Clear();

            _disposeCts.Dispose();
            _cache.Clear();

            if (_isClientOwned)
                _httpClient?.Dispose();

            OnDownloadCompleted = null;
            GC.SuppressFinalize(this);
        }
    }


    /// <summary>
    /// Статический менеджер для управления экземплярами vMixHttpBatcher.
    /// Предоставляет общий HttpClient для всех запросов для повышения эффективности.
    /// </summary>
    public static class APIRequestManagerV2
    {
        private sealed class BatcherEntry
        {
            private long _lastAccessTicks;
            public readonly vMixHttpBatcher Batcher;

            public BatcherEntry(vMixHttpBatcher batcher)
            {
                Batcher = batcher ?? throw new ArgumentNullException(nameof(batcher));
                _lastAccessTicks = DateTime.UtcNow.Ticks;
            }

            public DateTime LastAccess
            {
                get => new DateTime(Interlocked.Read(ref _lastAccessTicks), DateTimeKind.Utc);
                set => Interlocked.Exchange(ref _lastAccessTicks, value.Ticks);
            }
        }

        private static readonly HttpClient _sharedClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        private static readonly object _batchersGate = new object();
        private static readonly ConcurrentDictionary<string, Lazy<BatcherEntry>> _batchers = new ConcurrentDictionary<string, Lazy<BatcherEntry>>();
        private static readonly TimeSpan _inactiveTimeout = TimeSpan.FromMinutes(1);
        private static Timer _cleanupTimer;

        static APIRequestManagerV2()
        {
            _cleanupTimer = new Timer(_ => CleanupInactiveBatchers(), null, _inactiveTimeout, _inactiveTimeout);
        }

        private static string ResolveBatcherKey(string apiUrl, bool post = false)
        {
            if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var uri))
                throw new ArgumentException($"Invalid URL: {apiUrl}", nameof(apiUrl));

            string category = uri.AbsolutePath.TrimEnd('/').EndsWith("/api")
                ? "state"
                : "function";

            string method = post ? "post" : "get";

            return $"{uri.Scheme}://{uri.Authority}/{category},{method}";
        }
        /// <summary>
        /// Получает ответ от vMix API. Управляет созданием и переиспользованием обработчиков запросов.
        /// </summary>
        /// <param name="apiUrl">Базовый URL API vMix (например, "http://127.0.0.1:8088/api")</param>
        /// <param name="callback">Необязательный callback для получения результата.</param>
        /// <returns>Задача, представляющая XML-ответ от vMix.</returns>
        public static Task<string> GetApiResponseAsync(string apiUrl, WeakAction callback = null, string auth = null, bool post = false, bool ignoreCache = false)
        {
            string key = ResolveBatcherKey(apiUrl, post);

            lock (_batchersGate)
            {
                var lazy = _batchers.GetOrAdd(key, _ => new Lazy<BatcherEntry>(
                    () => new BatcherEntry(new vMixHttpBatcher(_sharedClient)),
                    LazyThreadSafetyMode.ExecutionAndPublication));

                var entry = lazy.Value;
                entry.LastAccess = DateTime.UtcNow;

                return entry.Batcher.GetStringAsync(new Uri(apiUrl), callback, post, ignoreCache, auth);
            }
        }

        private static void CleanupInactiveBatchers()
        {
            var now = DateTime.UtcNow;
            var toDispose = new List<Lazy<BatcherEntry>>();

            lock (_batchersGate)
            {
                var inactiveKeys = _batchers
                    .Where(kvp =>
                    {
                        if (!kvp.Value.IsValueCreated) return false;
                        var entry = kvp.Value.Value;
                        return (now - entry.LastAccess) > _inactiveTimeout
                               && !entry.Batcher.IsProcessing();
                    })
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in inactiveKeys)
                {
                    if (!_batchers.TryGetValue(key, out var lazy) || !lazy.IsValueCreated)
                        continue;

                    var entry = lazy.Value;
                    if ((now - entry.LastAccess) > _inactiveTimeout
                        && !entry.Batcher.IsProcessing())
                    {
                        if (_batchers.TryRemove(key, out var removed) && removed.IsValueCreated)
                            toDispose.Add(removed);
                    }
                }
            }

            // Dispose вне lock
            foreach (var removed in toDispose)
                removed.Value.Batcher.Dispose();
        }

        /// <summary>
        /// Принудительно освобождает все ресурсы, используемые менеджером.
        /// Вызывать при завершении работы приложения.
        /// </summary>
        public static void Cleanup()
        {
            List<Lazy<BatcherEntry>> toDispose;

            lock (_batchersGate)
            {
                _cleanupTimer?.Dispose();
                _cleanupTimer = null;

                toDispose = _batchers.Values.ToList();
                _batchers.Clear();
            }

            foreach (var lazy in toDispose)
            {
                if (lazy.IsValueCreated)
                    lazy.Value.Batcher.Dispose();
            }
        }
    }

}