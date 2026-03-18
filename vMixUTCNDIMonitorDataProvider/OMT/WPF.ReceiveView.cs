using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace UTCNDIMonitorDataProvider.OMT
{
    public class ReceiveView : Viewbox, IDisposable, INotifyPropertyChanged
    {
        [Category("OMT")]
        [Description("The name of this receiver channel.")]
        public string ReceiverName
        {
            get => (string)GetValue(ReceiverNameProperty);
            set => SetValue(ReceiverNameProperty, value);
        }

        public static readonly DependencyProperty ReceiverNameProperty =
            DependencyProperty.Register(nameof(ReceiverName), typeof(string), typeof(ReceiveView), new PropertyMetadata(""));

        [Category("OMT")]
        [Description("The OMT source to connect to. Empty string disconnects.")]
        public string ConnectedSource
        {
            get => (string)GetValue(ConnectedSourceProperty);
            set => SetValue(ConnectedSourceProperty, value);
        }

        public static readonly DependencyProperty ConnectedSourceProperty =
            DependencyProperty.Register(nameof(ConnectedSource), typeof(string), typeof(ReceiveView), new PropertyMetadata("", OnConnectedSourceChanged));

        [Category("OMT")]
        [Description("If true (default) received audio will be sent to the default Windows audio playback device.")]
        public bool IsAudioEnabled
        {
            get => (bool)GetValue(IsAudioEnabledProperty);
            set => SetValue(IsAudioEnabledProperty, value);
        }

        public static readonly DependencyProperty IsAudioEnabledProperty =
            DependencyProperty.Register(nameof(IsAudioEnabled), typeof(bool), typeof(ReceiveView), new PropertyMetadata(true, OnAudioEnabledChange));

        private static void OnAudioEnabledChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ReceiveView)d)._audioEnabled = (bool)e.NewValue;
        }

        [Category("OMT")]
        [Description("If true (default) received video will be sent to the screen.")]
        public bool IsVideoEnabled
        {
            get => _videoEnabled;
            set
            {
                if (value != _videoEnabled)
                {
                    _videoEnabled = value;
                    NotifyPropertyChanged(nameof(IsVideoEnabled));
                }
            }
        }

        [Category("OMT")]
        [Description("Set or get the current audio volume. Range is 0.0 to 1.0")]
        public float Volume
        {
            get => _volume;
            set
            {
                float clamped = Math.Max(0.0f, Math.Min(1.0f, value));
                if (Math.Abs(clamped - _volume) > 0.0001f)
                {
                    _volume = clamped;
                    NotifyPropertyChanged(nameof(Volume));
                }
            }
        }

        [Category("OMT")]
        [Description("Is current source using low bandwidth?")]
        public bool IsLowBandwidth
        {
            get => (bool)GetValue(IsLowBandwidthProperty);
            set => SetValue(IsLowBandwidthProperty, value);
        }

        public static readonly DependencyProperty IsLowBandwidthProperty =
            DependencyProperty.Register(nameof(IsLowBandwidth), typeof(bool), typeof(ReceiveView), new PropertyMetadata(true, OnLowBandwidthChanged));

        private static void OnLowBandwidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ReceiveView)d).Reconnect();
        }

        public ReceiveView()
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

        private void Reconnect()
        {
            if (_disposed)
                return;

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
            if (_disposed)
                return;

            _disposed = true;

            Interlocked.Exchange(ref _receiverId, 0);
            _exitThread = true;

            if (disposing)
            {
                if (_receiveThread != null)
                {
                    try
                    {
                        _receiveThread.Join();
                    }
                    catch
                    {
                    }

                    _receiveThread = null;
                }
            }

            lock (_receiverLock)
            {
                if (_receiver != null)
                {
                    try
                    {
                        _receiver.Dispose();
                    }
                    catch
                    {
                    }

                    _receiver = null;
                }
            }
        }

        private bool _disposed = false;

        private static void OnConnectedSourceChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is ReceiveView view)
                view.Connect(view.ConnectedSource);
        }

        private void Connect(string source)
        {
            int receiverId = Interlocked.Increment(ref _receiverId);

            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            if (Child == null)
                Child = VideoSurface;

            Disconnect();

            if (string.IsNullOrWhiteSpace(source))
                return;

            lock (_receiverLock)
            {
                _receiver = new libomtnet.OMTReceive(
                    source,
                    libomtnet.OMTFrameType.Video,
                    libomtnet.OMTPreferredVideoFormat.BGRA,
                    IsLowBandwidth ? libomtnet.OMTReceiveFlags.Preview : libomtnet.OMTReceiveFlags.None);
            }

            _exitThread = false;
            _receiveThread = new Thread(ReceiveThreadProc)
            {
                IsBackground = true,
                Name = "OmtExampleReceiveThread"
            };
            _receiveThread.Start(receiverId);
        }

        public void Disconnect()
        {
            _exitThread = true;

            if (_receiveThread != null)
            {
                try
                {
                    _receiveThread.Join();
                }
                catch
                {
                }

                _receiveThread = null;
            }

            lock (_receiverLock)
            {
                if (_receiver != null)
                {
                    try
                    {
                        _receiver.Dispose();
                    }
                    catch
                    {
                    }

                    _receiver = null;
                }
            }

            _exitThread = false;
            Interlocked.Exchange(ref _renderPending, 0);
            Interlocked.Exchange(ref _frameReady, 0);
        }

        private void ReceiveThreadProc(object param)
        {
            int currReceiverId = (int)param;
            libomtnet.OMTMediaFrame videoFrame = new libomtnet.OMTMediaFrame();

            while (!_exitThread)
            {
                libomtnet.OMTReceive receiver;
                lock (_receiverLock)
                {
                    receiver = _receiver;
                }

                if (receiver == null)
                    break;

                bool ok = false;
                try
                {
                    ok = receiver.Receive(libomtnet.OMTFrameType.Video, 1000, ref videoFrame);
                }
                catch
                {
                    Thread.Sleep(100);
                    continue;
                }

                if (!ok)
                    continue;

                if (!_videoEnabled || videoFrame.Data == IntPtr.Zero)
                    continue;

                if (currReceiverId != _receiverId || _receiverId == 0 || _disposed)
                    continue;

                int xres = (int)videoFrame.Width;
                int yres = (int)videoFrame.Height;
                int stride = (int)videoFrame.Stride;

                if (xres <= 0 || yres <= 0 || stride <= 0)
                    continue;

                int bufferSize = yres * stride;
                double dpiX = 96.0 * (videoFrame.AspectRatio / ((double)xres / yres));

                lock (_frameLock)
                {
                    EnsureFrameBuffer(bufferSize);

                    Marshal.Copy(videoFrame.Data, _latestFrameBuffer, 0, bufferSize);

                    _latestWidth = xres;
                    _latestHeight = yres;
                    _latestStride = stride;
                    _latestBufferSize = bufferSize;
                    _latestDpiX = dpiX;

                    Interlocked.Exchange(ref _frameReady, 1);
                }

                ScheduleRender(currReceiverId);
            }
        }

        private void ScheduleRender(int currReceiverId)
        {
            if (Interlocked.Exchange(ref _renderPending, 1) == 1)
                return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (currReceiverId != _receiverId || _receiverId == 0 || _disposed)
                        return;

                    if (Interlocked.Exchange(ref _frameReady, 0) == 0)
                        return;

                    int width;
                    int height;
                    int stride;
                    int bufferSize;
                    double dpiX;
                    byte[] frameCopy;

                    lock (_frameLock)
                    {
                        width = _latestWidth;
                        height = _latestHeight;
                        stride = _latestStride;
                        bufferSize = _latestBufferSize;
                        dpiX = _latestDpiX;

                        if (_latestFrameBuffer == null || bufferSize <= 0)
                            return;

                        EnsureUiBuffer(bufferSize);
                        Buffer.BlockCopy(_latestFrameBuffer, 0, _uiFrameBuffer, 0, bufferSize);
                        frameCopy = _uiFrameBuffer;
                    }

                    if (VideoBitmap == null ||
                        VideoBitmap.PixelWidth != width ||
                        VideoBitmap.PixelHeight != height ||
                        Math.Abs(VideoBitmap.DpiX - dpiX) > 0.001)
                    {
                        VideoBitmap = new WriteableBitmap(width, height, dpiX, 96.0, PixelFormats.Bgra32, null);
                        VideoSurface.Source = VideoBitmap;
                    }

                    VideoBitmap.WritePixels(
                        new Int32Rect(0, 0, width, height),
                        frameCopy,
                        stride,
                        0);
                }
                finally
                {
                    Interlocked.Exchange(ref _renderPending, 0);

                    if (Volatile.Read(ref _frameReady) == 1 && !_disposed && _receiverId != 0)
                        ScheduleRender(currReceiverId);
                }
            }), DispatcherPriority.Render);
        }

        private void EnsureFrameBuffer(int requiredSize)
        {
            if (_latestFrameBuffer == null || _latestFrameBuffer.Length < requiredSize)
                _latestFrameBuffer = new byte[requiredSize];
        }

        private void EnsureUiBuffer(int requiredSize)
        {
            if (_uiFrameBuffer == null || _uiFrameBuffer.Length < requiredSize)
                _uiFrameBuffer = new byte[requiredSize];
        }

        private readonly object _receiverLock = new object();
        private readonly object _frameLock = new object();

        private libomtnet.OMTReceive _receiver = null;
        private Thread _receiveThread = null;
        private volatile bool _exitThread = false;

        private readonly Image VideoSurface = new Image();
        private WriteableBitmap VideoBitmap;

        private bool _audioEnabled = false;
        private bool _videoEnabled = true;
        private float _volume = 1.0f;

        private int _receiverId = 0;
        private int _renderPending = 0;
        private int _frameReady = 0;

        private byte[] _latestFrameBuffer;
        private byte[] _uiFrameBuffer;
        private int _latestWidth;
        private int _latestHeight;
        private int _latestStride;
        private int _latestBufferSize;
        private double _latestDpiX;
    }
}