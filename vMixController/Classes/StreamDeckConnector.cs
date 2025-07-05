using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace vMixController.Classes
{
    public struct StreamDeckEvent
    {
        public vMixStreamDeckLibrary.StreamDeckEvent Type;
        public string Context;
    }
    public class StreamDeckConnector : IDisposable
    {
        public event EventHandler<StreamDeckEvent> OnStreamDeckEvent;
        private bool _disposed = false;
        public StreamDeckConnector()
        {
            Task.Run(() =>
            {
                while (!_disposed)
                {
                    using (var r = new MemoryMessagePipe.MemoryMappedFileMessageReceiver("vMixUTCMMF"))
                    {

                        Dispatcher.CurrentDispatcher.Invoke(() =>
                        {
                            var msg = r.ReceiveMessage<StreamDeckEvent>(stream =>
                            {
                                var result = new StreamDeckEvent();
                                result.Type = (vMixStreamDeckLibrary.StreamDeckEvent)stream.ReadByte();
                                byte[] buffer = new byte[1024];
                                var len = stream.Read(buffer, 0, 1024);
                                result.Context = Encoding.UTF8.GetString(buffer, 0, len);
                                return result;
                            });
                            if (msg.Context != null && msg.Type != vMixStreamDeckLibrary.StreamDeckEvent.None) 
                                OnStreamDeckEvent?.Invoke(this, msg);
                        });


                    }
                    Thread.Sleep(100);
                }



            });

        }
        public void Dispose()
        {
            _disposed = true;
        }
    }
}
