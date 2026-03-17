using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.TextFormatting;
using System.Windows.Threading;
using System.Xml;
using vMixAPI;
using vMixController.Widgets;

namespace vMixController.Classes
{
    public static class XmlDocumentMessenger
    {
        public delegate void DocumentDownloaded(XmlDocument doc, DateTime timestamp);

        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        private static readonly object _stateLock = new object();

        private static CancellationTokenSource _cts;
        private static Task _pollLoopTask;

        private static string _url = "http://127.0.0.1:8088/api";
        private static string _credentials;

        private static int _activeRequests;

        public static bool Sync { get; set; } = true;

        public static string Url
        {
            get { return _url; }
            set { _url = (value ?? "http://127.0.0.1:8088/api").TrimEnd('?'); }
        }

        /// <summary>
        /// Формат: "user:password"
        /// </summary>
        public static string Credentials
        {
            get { return _credentials; }
            set { _credentials = value; }
        }

        public static int Rate { get; set; }

        public static int MaxConcurrentRequests { get; set; } = 1;

        private static event DocumentDownloaded _onDocumentDownloaded;
        public static event DocumentDownloaded OnDocumentDownloaded
        {
            add
            {
                _onDocumentDownloaded += value;
                Debug.WriteLine("XmlDocumentMessenger subscribers: " + (_onDocumentDownloaded == null ? 0 : _onDocumentDownloaded.GetInvocationList().Length));
            }
            remove
            {
                _onDocumentDownloaded -= value;
                Debug.WriteLine("XmlDocumentMessenger subscribers: " + (_onDocumentDownloaded == null ? 0 : _onDocumentDownloaded.GetInvocationList().Length));
            }
        }

        public static event Action<Exception> OnError;

        public static void Start()
        {
            lock (_stateLock)
            {
                if (_pollLoopTask != null && !_pollLoopTask.IsCompleted)
                    return;

                _cts = new CancellationTokenSource();
                _pollLoopTask = Task.Run(() => PollLoopAsync(_cts.Token));
            }
        }

        public static async Task StopAsync()
        {
            Task taskToWait = null;

            lock (_stateLock)
            {
                if (_cts == null)
                    return;

                _cts.Cancel();
                taskToWait = _pollLoopTask;
            }

            try
            {
                if (taskToWait != null)
                    await taskToWait.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // expected
            }
            finally
            {
                lock (_stateLock)
                {
                    if (_cts != null)
                    {
                        _cts.Dispose();
                        _cts = null;
                    }
                    _pollLoopTask = null;
                }
            }
        }

        private static async Task PollLoopAsync(CancellationToken token)
        {
            var stopwatch = Stopwatch.StartNew();
            TimeSpan nextPollAt = TimeSpan.Zero;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (!Sync || _onDocumentDownloaded == null)
                    {
                        await Task.Delay(200, token).ConfigureAwait(false);
                        continue;
                    }

                    TimeSpan pollInterval = GetPollInterval();

                    if (stopwatch.Elapsed >= nextPollAt)
                    {
                        // fire-and-forget, ограничено MaxConcurrentRequests
                        PollOnceAsync(token).ConfigureAwait(false);
                        nextPollAt = stopwatch.Elapsed + pollInterval;
                    }

                    TimeSpan delay = nextPollAt - stopwatch.Elapsed;
                    if (delay < TimeSpan.FromMilliseconds(50))
                        delay = TimeSpan.FromMilliseconds(50);

                    await Task.Delay(delay, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    if (token.IsCancellationRequested)
                        break;
                }
                catch (Exception ex)
                {
                    RaiseError(ex);
                    await Task.Delay(300, token).ConfigureAwait(false);
                }
            }
        }

        private static async Task PollOnceAsync(CancellationToken token)
        {
            if (Interlocked.Increment(ref _activeRequests) > MaxConcurrentRequests)
            {
                Interlocked.Decrement(ref _activeRequests);
                return;
            }

            try
            {
                Uri uri;
                if (!Uri.TryCreate(Url, UriKind.Absolute, out uri))
                    return;

                using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                {
                    if (!string.IsNullOrWhiteSpace(Credentials))
                    {
                        var raw = Encoding.UTF8.GetBytes(Credentials);
                        var base64 = Convert.ToBase64String(raw);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64);
                    }

                    using (var response = await _httpClient.SendAsync(request, token).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();

                        var xmlText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (string.IsNullOrWhiteSpace(xmlText))
                            return;

                        var doc = new XmlDocument();
                        doc.LoadXml(xmlText);

                        if (!string.Equals(doc.DocumentElement != null ? doc.DocumentElement.Name : null, "vmix", StringComparison.OrdinalIgnoreCase))
                            return;

                        RaiseDocumentDownloaded(doc, DateTime.Now);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (!token.IsCancellationRequested)
                    RaiseError(new TimeoutException("Request canceled unexpectedly."));
            }
            catch (Exception ex)
            {
                RaiseError(ex);
            }
            finally
            {
                Interlocked.Decrement(ref _activeRequests);
            }
        }

        private static TimeSpan GetPollInterval()
        {
            double ms = (Rate != 0)
                ? Properties.Settings.Default.AudioMeterPollTime * 1000.0
                : vMixControl.ShadowUpdatePollTime.TotalMilliseconds;

            if (ms < 50) ms = 50;
            return TimeSpan.FromMilliseconds(ms);
        }

        private static void RaiseDocumentDownloaded(XmlDocument doc, DateTime timestamp)
        {
            var handler = _onDocumentDownloaded;
            if (handler == null) return;

            var dispatcher = Application.Current != null ? Application.Current.Dispatcher : null;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(new Action(() => handler(doc, timestamp)));
            }
            else
            {
                handler(doc, timestamp);
            }
        }

        private static void RaiseError(Exception ex)
        {
            Debug.WriteLine("XmlDocumentMessenger error: " + ex);
            var errorHandler = OnError;
            if (errorHandler != null)
                errorHandler(ex);
        }
    }
}
