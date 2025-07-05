using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace vMixAPI
{
    public class vMixWebClient : WebClient
    {
        //public static int _requests = 0;
        public int Requests { get; set; }
        public int Timeout { get; set; }
        public string Tag { get; set; }
        public bool Working { get; set; }
        private int _requestsCount = 0;
        private string _cache = null;

        public vMixWebClient()
        {
            this.Timeout = 5000;
            this.Encoding = Encoding.UTF8;
            this.DownloadStringCompleted += VMixWebClient_DownloadStringCompleted;
        }

        private void VMixWebClient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error == null)
                _cache = e.Result;
            Working = false;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest webRequest = base.GetWebRequest(address);
            ((HttpWebRequest)webRequest).KeepAlive = false;
            webRequest.Timeout = Timeout;
            //Debug.WriteLine(++_requests);
            return webRequest;
        }

        protected override void Dispose(bool disposing)
        {
            //Debug.WriteLine(--_requests);
            base.Dispose(disposing);
        }

        public new void DownloadStringAsync(Uri address, object userToken)
        {
            if (!Working)
            {
                Working = true;
                base.DownloadStringAsync(address, userToken);
            }
            Requests++;
            _requestsCount++;
            //Debug.WriteLine("Requesting {0} : {1}", Tag, _requestsCount);
        }
    }

    public static class APIRequestManager
    {
        
        private static ConcurrentDictionary<string, vMixWebClient> _clients = new ConcurrentDictionary<string, vMixWebClient>();
        public static event EventHandler OnStatusChange;

        public static vMixWebClient GetClient(string url, int timeout) {
            Debug.WriteLine("WebClients : {0}", _clients.Count);
            string trimmedUrl = url.TrimEnd('?');
            OnStatusChange?.Invoke(null, null);
            if (_clients.ContainsKey(trimmedUrl))
            {
                /*vMixWebClient client = null;
                if (_clients.TryGetValue(trimmedUrl, out client))
                    return client;*/
                    
                //Fallback to old behavior
                return new vMixWebClient();
            }
            else
            {
                /*vMixWebClient _webClient = new vMixWebClient();
                _webClient.Timeout = timeout;
                _webClient.DownloadStringCompleted += _webClient_DownloadStringCompleted;
                _webClient.Tag = trimmedUrl;
                if (_clients.TryAdd(trimmedUrl, _webClient))
                    return _webClient;
                else*/
                    //Fallback to old behavior
                    return new vMixWebClient();
            }
        }

        private static void _webClient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            vMixWebClient client = null;
            if (((vMixWebClient)sender).Requests > 5)
                _clients.TryRemove(((vMixWebClient)sender).Tag, out client);
            OnStatusChange?.Invoke(null, null);
        }
    }
}
