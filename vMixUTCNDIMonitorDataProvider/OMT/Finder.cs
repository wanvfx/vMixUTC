using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Data;

namespace UTCNDIMonitorDataProvider.OMT
{
    public class Finder : IDisposable
    {
        public ObservableCollection<string> Sources
        {
            get { return _sourceList; }
        }

        public Finder()
        {
            BindingOperations.EnableCollectionSynchronization(_sourceList, _sourceLock);

            IntPtr groupsNamePtr = IntPtr.Zero;

            // start up a thread to update on
            _findThread = new Thread(FindThreadProc) { IsBackground = true, Name = "OmtFindThread" };
            _findThread.Start();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Finder()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // tell the thread to exit
                    _exitThread = true;

                    // wait for it to exit
                    if (_findThread != null)
                    {
                        _findThread.Join();

                        _findThread = null;
                    }
                }

                if (_findInstancePtr != IntPtr.Zero)
                {
                    _findInstancePtr = IntPtr.Zero;
                }

                _disposed = true;
            }
        }

        private bool _disposed = false;

        private void FindThreadProc()
        {
            while (!_exitThread)
            {
                
                foreach (var name in libomtnet.OMTDiscovery.GetInstance().GetAddresses())
                    if (!_sourceList.Any(item => item == name))
                    {
                        _sourceList.Add(name);
                    }
            }

        }

        private IntPtr _findInstancePtr = IntPtr.Zero;

        private ObservableCollection<string> _sourceList = new ObservableCollection<string>();
        private object _sourceLock = new object();

        // a thread to find on so that the UI isn't dragged down
        Thread _findThread = null;

        // a way to exit the thread safely
        bool _exitThread = false;
    }
}
