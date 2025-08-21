using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        public static bool Sync { get; set; }
        public static string Url
        {
            get => url; set
            {
                _queries = 0;
                url = value;
            }
        }

        public static string Credentials { get => credentials; set { credentials = value; } }

        public delegate void DocumentDownloaded(XmlDocument doc, DateTime timestamp);
        static int _subscribers = 0;

        public static int Rate { get; set; }

        static event DocumentDownloaded _onDocumentDownloaded;
        public static event DocumentDownloaded OnDocumentDownloaded
        {
            add
            {
                _onDocumentDownloaded += value;
                _subscribers = _onDocumentDownloaded?.GetInvocationList().Length ?? 0;
                Debug.Print("{0} subscribers", _subscribers);
            }
            remove
            {
                _onDocumentDownloaded -= value;
                _subscribers = _onDocumentDownloaded?.GetInvocationList().Length ?? 0;
                Debug.Print("{0} subscribers", _subscribers);
            }
        }

        static int _queries = 0;
        static DateTime _previousQuery = DateTime.Now;
        static System.Threading.Timer _stateDependentTimer;
        private static string url;
        private static string credentials;

        static XmlDocumentMessenger()
        {
            _stateDependentTimer = new System.Threading.Timer((obj) =>
            {
                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    if (!Sync) return;

                    var t = DateTime.Now - _previousQuery;
                    var pollInterval = (Rate != 0 ? Properties.Settings.Default.AudioMeterPollTime * 1000 : vMixControl.ShadowUpdatePollTime.TotalMilliseconds);
                    if (t.TotalMilliseconds >= pollInterval && _queries < 5 && _subscribers > 0)
                    {
                        _previousQuery = DateTime.Now;
                        _queries++;

                        Uri uri = null;
                        if (Uri.TryCreate((Url ?? "http://127.0.0.1:8088/api").TrimEnd('?'), UriKind.Absolute, out uri))
                        {
                            APIRequestManagerV2.GetApiResponseAsync(uri.ToString(), new WeakAction((response, ex) =>
                            {
                                Interlocked.Decrement(ref _queries);

                                Dispatcher.CurrentDispatcher.Invoke(() =>
                                {
                                    if (ex != null)
                                        return;

                                    try
                                    {
                                        if (!string.IsNullOrWhiteSpace(response) && response.StartsWith("<vmix>"))
                                        {
                                            XmlDocument doc = new XmlDocument();
                                            doc.LoadXml(response);
                                            _onDocumentDownloaded?.Invoke(doc, DateTime.Now);
                                        }
                                    }
                                    catch (Exception)
                                    {

                                    }
                                });
                            }), credentials);
                        }
                        else
                        {
                            _queries--;
                        }
                    }
                });
            }, null, 0, 10);
        }


    }
}
