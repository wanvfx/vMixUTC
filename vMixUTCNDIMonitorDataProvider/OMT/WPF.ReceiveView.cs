using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace UTCNDIMonitorDataProvider.OMT
{
    // If you do not use this control, you can remove this file
    // and remove the dependency on naudio.
    // Alternatively you can also remove any naudio related entries
    // and use it for video only, but don't forget that you will still need
    // to free any audio frames received.
    public class ReceiveView : Viewbox, IDisposable, INotifyPropertyChanged
    {
        [Category("OMT"),
        Description("The name of this receiver channel. Required or else an invalid argument exception will be thrown.")]
        public String ReceiverName
        {
            get { return (String)GetValue(ReceiverNameProperty); }
            set { SetValue(ReceiverNameProperty, value); }
        }
        public static readonly DependencyProperty ReceiverNameProperty =
            DependencyProperty.Register("ReceiverName", typeof(String), typeof(ReceiveView), new PropertyMetadata(""));



        [Category("OMT"),
        Description("The NDI source to connect to. An empty new Source() or a Source with no Name will disconnect.")]
        public string ConnectedSource
        {
            get { return (string)GetValue(ConnectedSourceProperty); }
            set { SetValue(ConnectedSourceProperty, value); }
        }
        public static readonly DependencyProperty ConnectedSourceProperty =
            DependencyProperty.Register("ConnectedSource", typeof(string), typeof(ReceiveView), new PropertyMetadata("", OnConnectedSourceChanged));


        [Category("OMT"),
        Description("If true (default) received audio will be sent to the default Windows audio playback device.")]
        public bool IsAudioEnabled
        {
            get { return (bool)GetValue(IsAudioEnabledProperty); ; }
            set
            {
                SetValue(IsAudioEnabledProperty, value);
            }
        }
        public static readonly DependencyProperty IsAudioEnabledProperty =
    DependencyProperty.Register("IsAudioEnabled", typeof(bool), typeof(ReceiveView), new PropertyMetadata(true, OnAudioEnabledChange));

        private static void OnAudioEnabledChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ReceiveView)d)._audioEnabled = (bool)e.NewValue;
        }

        [Category("OMT"),
        Description("If true (default) received video will be sent to the screen.")]
        public bool IsVideoEnabled
        {
            get { return _videoEnabled; }
            set
            {
                if (value != _videoEnabled)
                {
                    _videoEnabled = value;
                    NotifyPropertyChanged("IsVideoEnabled");
                }
            }
        }

        [Category("OMT"),
        Description("Set or get the current audio volume. Range is 0.0 to 1.0")]
        public float Volume
        {
            get { return _volume; }
            set
            {
                if (value != _volume)
                {
                    _volume = Math.Max(0.0f, Math.Min(1.0f, value));

                    //if (_wasapiOut != null)
                    //_wasapiOut.Volume = _volume;

                    NotifyPropertyChanged("Volume");
                }
            }
        }


        [Category("OMT"),
        Description("Is current source using low bandwidth?")]
        public bool IsLowBandwidth
        {
            get { return (bool)GetValue(IsLowBandwidthProperty); }
            set { SetValue(IsLowBandwidthProperty, value); }
        }
        public static readonly DependencyProperty IsLowBandwidthProperty =
            DependencyProperty.Register("IsLowBandwidth", typeof(bool), typeof(ReceiveView), new PropertyMetadata(true, OnPropertyChangedCallback));

        private static void OnPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ReceiveView)d).Reconnect();
        }

        public ReceiveView()
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                return;
        }

        public event PropertyChangedEventHandler PropertyChanged;


        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private void Reconnect()
        {
            Disconnect();
            Connect(ConnectedSource);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ReceiveView()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // This call happens when the window is closing, so we set the
                    // Id to 0 to signal we don't want to process any more frames.
                    Interlocked.Exchange(ref _receiverId, 0);

                    // tell the thread to exit
                    _exitThread = true;

                    // wait for it to exit
                    if (_receiveThread != null)
                    {
                        _receiveThread.Join();

                        _receiveThread = null;
                    }

                }

                // Destroy the receiver
                if (_receiver != null)
                {
                    _receiver.Dispose();
                }

                _disposed = true;
            }
        }

        private bool _disposed = false;

        // when the ConnectedSource changes, connect to it.
        private static void OnConnectedSourceChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ReceiveView s = sender as ReceiveView;
            if (s == null)
                return;

            s.Connect(s.ConnectedSource);
        }

        // connect to an NDI source in our Dictionary by name
        private void Connect(string source)
        {
            // Increment the receiver Id, meaning we have a new source to work with. If
            // there's already another receiver thread running, the commands it has
            // sent to the UI won't be processed.
            int receiverId = Interlocked.Increment(ref _receiverId);

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                return;

            // before we are connected, we need to set up our image
            // it's bad practice to do this in the constructor
            if (Child == null)
                Child = VideoSurface;

            // just to be safe
            Disconnect();

            // Sanity
            if (source == null || String.IsNullOrEmpty(source))
                return;

            /*if (String.IsNullOrEmpty(ReceiverName))
                throw new ArgumentException("ReceiverName can not be null or empty.", ReceiverName);*/


            _receiver = new libomtnet.OMTReceive(source, libomtnet.OMTFrameType.Video, libomtnet.OMTPreferredVideoFormat.BGRA, IsLowBandwidth ? libomtnet.OMTReceiveFlags.Preview : libomtnet.OMTReceiveFlags.None);


            _receiveThread = new Thread(ReceiveThreadProc) { IsBackground = true, Name = "OmtExampleReceiveThread" };
            // Pass the current receiver Id to the new thread
            _receiveThread.Start(receiverId);
        }

        public void Disconnect()
        {
            // check for a running thread
            if (_receiveThread != null)
            {
                // tell it to exit
                _exitThread = true;

                // wait for it to end
                _receiveThread.Join();
            }

            // reset thread defaults
            _receiveThread = null;
            _exitThread = false;

            // Destroy the receiver
            if (_receiver != null)
                _receiver.Dispose();

            // set it to a safe value
            _receiver = null;
        }

        // the receive thread runs though this loop until told to exit
        void ReceiveThreadProc(object param)
        {
            // Here we keep track of the receiver Id used for this thread.
            int currReceiverId = (int)param;

            while (!_exitThread && _receiver != null)
            {
                // The descriptors
                libomtnet.OMTMediaFrame videoFrame = new libomtnet.OMTMediaFrame();

                /*if (IsLowBandwidth)
                    _receiver.SetFlags(libomtnet.OMTReceiveFlags.Preview);
                else
                    _receiver.SetFlags(libomtnet.OMTReceiveFlags.None);*/

                if (_receiver.Receive(libomtnet.OMTFrameType.Video, 1000, ref videoFrame))
                {

                    // if not enabled, just discard
                    // this can also occasionally happen when changing sources
                    if (!_videoEnabled || videoFrame.Data == IntPtr.Zero)
                    {
                        break;
                    }

                    // get all our info so that we can free the frame
                    int yres = (int)videoFrame.Height;
                    int xres = (int)videoFrame.Width;

                    // quick and dirty aspect ratio correction for non-square pixels - SD 4:3, 16:9, etc.
                    double dpiX = 96.0 * (videoFrame.AspectRatio / ((double)xres / (double)yres));

                    int stride = (int)videoFrame.Stride;
                    int bufferSize = yres * stride;

                    // We need to be on the UI thread to write to our bitmap
                    // Not very efficient, but this is just an example
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // If the local receiver Id is not the same as the global receiver Id,
                        // then that means that either the connection source has changed, or
                        // the window has closed, in which case the latest receiver Id
                        // will be 0. If either is true, we stop processing data.
                        if (currReceiverId != _receiverId
                                || _receiverId == 0)
                        {
                            return;
                        }

                        // resize the writeable if needed
                        if (VideoBitmap == null ||
                                VideoBitmap.PixelWidth != xres ||
                                VideoBitmap.PixelHeight != yres ||
                                Math.Abs(VideoBitmap.DpiX - dpiX) > 0.001)
                        {
                            VideoBitmap = new WriteableBitmap(xres, yres, dpiX, 96.0, PixelFormats.Pbgra32, null);
                            VideoSurface.Source = VideoBitmap;
                        }

                        // update the writeable bitmap
                        VideoBitmap.WritePixels(new Int32Rect(0, 0, xres, yres), videoFrame.Data, bufferSize, stride);

                        // free frames that were received AFTER use!
                        // This writepixels call is dispatched, so we must do it inside this scope.
                        //NDIlib.recv_free_video_v2(_recvInstancePtr, ref videoFrame);
                    }));


                }
            }
        }

        // a pointer to our unmanaged NDI receiver instance
        libomtnet.OMTReceive _receiver = null;

        // a thread to receive frames on so that the UI is still functional
        Thread _receiveThread = null;

        // a way to exit the thread safely
        bool _exitThread = false;

        // the image that will show our bitmap source
        private Image VideoSurface = new Image();

        // the bitmap source we copy received frames into
        private WriteableBitmap VideoBitmap;

        // should we send audio to Windows or not?
        private bool _audioEnabled = false;

        // should we send video to Windows or not?
        private bool _videoEnabled = true;

        // the current audio volume
        private float _volume = 1.0f;

        private String _webControlUrl = String.Empty;
        private String _receiverName = String.Empty;

        // This variable keeps track of the current Id of the receiver object. This
        // is a way to avoid processing frames on the UI thread when either the
        // connection source gets changed or the window closes.
        private int _receiverId = 0;
    }
}