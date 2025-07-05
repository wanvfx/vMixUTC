using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Threading;



namespace vMixAPI
{
    public class vMixHttpBatcher : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ConcurrentDictionary<string, (string response, Exception error, DateTime timestamp)> _responseCache
            = new ConcurrentDictionary<string, (string, Exception, DateTime)>();
        private readonly TimeSpan _cacheLifetime = TimeSpan.FromMilliseconds(500);

        private readonly ConcurrentQueue<(Uri uri, TaskCompletionSource<string> tcs, Action<string, Exception> callback)> _requestQueue
            = new ConcurrentQueue<(Uri, TaskCompletionSource<string>, Action<string, Exception>)>();
        private readonly System.Threading.Timer _batchTimer;
        private readonly System.Threading.Timer _clearCacheTimer;
        private readonly int _batchIntervalMs;
        private volatile bool _isProcessing;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private volatile bool _shouldExecuteImmediately = false;

        public event Action<string, Exception, Uri> OnDownloadCompleted;

        public vMixHttpBatcher(int batchIntervalMs = 100, HttpClient httpClient = null)
        {
            _batchIntervalMs = batchIntervalMs;
            bool initClient = httpClient == null;

            _httpClient = httpClient ?? new HttpClient();
            
            if (initClient)
                _httpClient.Timeout = TimeSpan.FromMilliseconds(batchIntervalMs * 2);

            _batchTimer = new System.Threading.Timer(_ => ProcessBatchAsync().ConfigureAwait(false),
                null, batchIntervalMs, batchIntervalMs);
            _clearCacheTimer = new System.Threading.Timer(_ => ClearExpiredCache(),
    null, batchIntervalMs / 10, batchIntervalMs / 10);
        }

        private bool ShouldCacheRequest(Uri uri)
        {
            // Кэшируем только запросы к /api без параметров
            return uri.AbsolutePath.Trim('?').EndsWith("/api", StringComparison.OrdinalIgnoreCase)
                   && string.IsNullOrEmpty(uri.Query);
        }


        public async Task<string> GetStringAsync(Uri address, Action<string, Exception> callback = null)
        {
            // Проверяем кэш только для запросов, которые должны кэшироваться
            if (ShouldCacheRequest(address) && _responseCache.TryGetValue(address.ToString(), out var cached))
            {
                callback?.Invoke(cached.response, cached.error);
                return cached.error != null
                    ? throw cached.error
                    : cached.response;
            }

            // Если запрос с параметрами - выполняем сразу без добавления в очередь
            if (!string.IsNullOrEmpty(address.Query) || _shouldExecuteImmediately)
            {
                if (ShouldCacheRequest(address))
                    _shouldExecuteImmediately = false;

                try
                {
                    var response = await _httpClient.GetStringAsync(address).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(address.Query) && response.Contains("Function completed successfully"))
                    {
                        _shouldExecuteImmediately = true;
                    }

                    callback?.Invoke(response, null);
                    Dispatcher.CurrentDispatcher.Invoke(() =>
                    {
                        OnDownloadCompleted?.Invoke(response, null, address);
                    });
                    return response;
                }
                catch (Exception ex)
                {
                    callback?.Invoke(null, ex);
                    Dispatcher.CurrentDispatcher.Invoke(() =>
                    {
                        OnDownloadCompleted?.Invoke(null, ex, address);
                    });
                    throw;
                }
            }

            // Для запросов без параметров добавляем в очередь
            var tcs = new TaskCompletionSource<string>();
            _requestQueue.Enqueue((address, tcs, callback));
            return await tcs.Task;
        }

        private async Task ProcessBatchAsync()
        {
            if (_isProcessing || _requestQueue.IsEmpty)
                return;

            await _semaphore.WaitAsync();
            _isProcessing = true;

            try
            {
                var requests = new Dictionary<Uri, List<(TaskCompletionSource<string> tcs, Action<string, Exception> callback)>>();
                while (_requestQueue.TryDequeue(out var item))
                {
                    if (!requests.ContainsKey(item.uri))
                    {
                        requests[item.uri] = new List<(TaskCompletionSource<string>, Action<string, Exception>)>();
                    }
                    requests[item.uri].Add((item.tcs, item.callback));
                }

                foreach (var kvp in requests)
                {
                    await ProcessSingleRequestAsync(kvp.Key, kvp.Value);
                }
            }
            finally
            {
                _isProcessing = false;
                _semaphore.Release();
            }
        }

        private async Task ProcessSingleRequestAsync(Uri uri, List<(TaskCompletionSource<string> tcs, Action<string, Exception> callback)> requests)
        {
            Exception error = null;
            string response = null;

            try
            {
                response = await _httpClient.GetStringAsync(uri).ConfigureAwait(false);
                if (ShouldCacheRequest(uri))
                {
                    _responseCache[uri.ToString()] = (response, null, DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                error = ex;
                if (ShouldCacheRequest(uri))
                {
                    _responseCache[uri.ToString()] = (null, ex, DateTime.Now);
                }
            }

            foreach (var (tcs, callback) in requests)
            {
                if (error != null)
                {
                    tcs.TrySetException(error);
                    callback?.Invoke(null, error);
                }
                else
                {
                    tcs.TrySetResult(response);
                    callback?.Invoke(response, null);
                }
            }

            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                OnDownloadCompleted?.Invoke(response, error, uri);
            });
        }

        public void ClearExpiredCache()
        {
            var now = DateTime.Now;
            foreach (var key in _responseCache.Keys)
            {
                if (_responseCache.TryGetValue(key, out var entry))
                {
                    if (now - entry.timestamp > _cacheLifetime)
                    {
                        _responseCache.TryRemove(key, out _);
                    }
                }
            }
        }

        public void Dispose()
        {
            _batchTimer?.Dispose();
            _httpClient?.Dispose();
            _semaphore?.Dispose();
        }
    }

    public static class APIRequestManagerV2
    {

        private static readonly ConcurrentDictionary<string, vMixHttpBatcher> _batchers = new ConcurrentDictionary<string, vMixHttpBatcher>();
        private static readonly TimeSpan _defaultBatchInterval = TimeSpan.FromSeconds(1);
        private static readonly HttpClient _sharedClient = new HttpClient();

        public static Task<string> GetApiResponseAsync(string apiUrl, Action<string, Exception> callback = null)
        {
            var batcher = _batchers.GetOrAdd(apiUrl,
                url => new vMixHttpBatcher((int)_defaultBatchInterval.TotalMilliseconds, _sharedClient));

            var tcs = new TaskCompletionSource<string>();

            _ = GetApiResponseInternalAsync(batcher, apiUrl, tcs, callback);

            return tcs.Task;//batcher.GetStringAsync(new Uri(apiUrl), callback);
        }

        private static async Task GetApiResponseInternalAsync(vMixHttpBatcher client, string url,
        TaskCompletionSource<string> tcs, Action<string, Exception> callback)
        {
            try
            {
                var response = await client.GetStringAsync(new Uri(url), callback);
                tcs.SetResult(response);
                callback?.Invoke(response, null);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                callback?.Invoke(null, ex);
            }
        }

        public static void RegisterCompletionHandler(string apiUrl, Action<string, Exception, Uri> handler)
        {
            var batcher = _batchers.GetOrAdd(apiUrl,
                url => new vMixHttpBatcher((int)_defaultBatchInterval.TotalMilliseconds, _sharedClient));
            batcher.OnDownloadCompleted += handler;
        }

        public static void UnregisterCompletionHandler(string apiUrl, Action<string, Exception, Uri> handler)
        {
            if (_batchers.TryGetValue(apiUrl, out var batcher))
            {
                batcher.OnDownloadCompleted -= handler;
            }
        }

        public static void Cleanup()
        {
            foreach (var batcher in _batchers.Values)
            {
                batcher.Dispose();
            }
            _batchers.Clear();
        }
    }
}
