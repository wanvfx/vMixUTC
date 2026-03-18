using NAudio.Wave;
using NewTek;
using NewTek.NDI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NewTek.NDI.WPF
{
    public class ReceiveView : Viewbox, IDisposable, INotifyPropertyChanged
    {
        [Category("NewTek NDI")]
        [Description("The name of this receiver channel. Required or else an invalid argument exception will be thrown.")]
        public string ReceiverName
        {
            get => (string)GetValue(ReceiverNameProperty);
            set => SetValue(ReceiverNameProperty, value);
        }

        public static readonly DependencyProperty ReceiverNameProperty =
            DependencyProperty.Register(nameof(ReceiverName), typeof(string), typeof(ReceiveView), new PropertyMetadata(""));

        [Category("NewTek NDI")]
        [Description("The NDI source to connect to. An empty new Source() or a Source with no Name will disconnect.")]
        public Source ConnectedSource
        {
            get => (Source)GetValue(ConnectedSourceProperty);
            set => SetValue(ConnectedSourceProperty, value);
        }

        public static readonly DependencyProperty ConnectedSourceProperty =
            DependencyProperty.Register(nameof(ConnectedSource), typeof(Source), typeof(ReceiveView), new PropertyMetadata(new Source(), OnConnectedSourceChanged));

        [Category("NewTek NDI")]
        [Description("If true (default) received audio will be sent to the default Windows audio playback device.")]
        public bool IsAudioEnabled
        {
            get => (bool)GetValue(IsAudioEnabledProperty);
            set => SetValue(IsAudioEnabledProperty, value);
        }

        public static readonly DependencyProperty IsAudioEnabledProperty =
            DependencyProperty.Register(nameof(IsAudioEnabled), typeof(bool), typeof(ReceiveView), new PropertyMetadata(true, OnAudioEnabledChanged));

        private static void OnAudioEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ReceiveView)d;
            view._audioEnabled = (bool)e.NewValue;
        }

        [Category("NewTek NDI")]
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

        [Category("NewTek NDI")]
        [Description("Set or get the current audio volume. Range is 0.0 to 1.0")]
        public float Volume
        {
            get => _volume;
            set
            {
                var clamped = Math.Max(0.0f, Math.Min(1.0f, value));
                if (Math.Abs(clamped - _volume) > 0.0001f)
                {
                    _volume = clamped;

                    if (_wasapiOut != null)
                    {
                        try
                        {
                            _wasapiOut.Volume = _volume;
                        }
                        catch
                        {
                            // ignore device-specific failures
                        }
                    }

                    NotifyPropertyChanged(nameof(Volume));
                }
            }
        }

        [Category("NewTek NDI")]
        [Description("Does the current source support PTZ functionality?")]
        public bool IsPtz
        {
            get => _isPtz;
            private set
            {
                if (value != _isPtz)
                {
                    _isPtz = value;
                    NotifyPropertyChanged(nameof(IsPtz));
                }
            }
        }

        [Category("NewTek NDI")]
        [Description("Does the current source support record functionality?")]
        public bool IsRecordingSupported
        {
            get => _canRecord;
            private set
            {
                if (value != _canRecord)
                {
                    _canRecord = value;
                    NotifyPropertyChanged(nameof(IsRecordingSupported));
                }
            }
        }

        [Category("NewTek NDI")]
        [Description("The web control URL for the current device, as a String, or an Empty String if not supported.")]
        public string WebControlUrl
        {
            get => _webControlUrl;
            private set
            {
                if (value != _webControlUrl)
                {
                    _webControlUrl = value;
                    NotifyPropertyChanged(nameof(WebControlUrl));
                }
            }
        }

        [Category("NewTek NDI")]
        [Description("Is current source using low bandwidth?")]
        public bool IsLowBandwidth
        {
            get => (bool)GetValue(IsLowBandwidthProperty);
            set => SetValue(IsLowBandwidthProperty, value);
        }

        public static readonly DependencyProperty IsLowBandwidthProperty =
            DependencyProperty.Register(nameof(IsLowBandwidth), typeof(bool), typeof(ReceiveView), new PropertyMetadata(true));

        public ReceiveView()
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region PTZ Methods

        public bool SetPtzZoom(double value)
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_zoom(_recvInstancePtr, (float)value);
        }

        public bool SetPtzZoomSpeed(double value)
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_zoom_speed(_recvInstancePtr, (float)value);
        }

        public bool SetPtzPanTilt(double pan, double tilt)
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_pan_tilt(_recvInstancePtr, (float)pan, (float)tilt);
        }

        public bool SetPtzPanTiltSpeed(double panSpeed, double tiltSpeed)
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_pan_tilt_speed(_recvInstancePtr, (float)panSpeed, (float)tiltSpeed);
        }

        public bool PtzStorePreset(int index)
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero || index < 0 || index > 99)
                return false;

            return NDIlib.recv_ptz_store_preset(_recvInstancePtr, index);
        }

        public bool PtzRecallPreset(int index, double speed)
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero || index < 0 || index > 99)
                return false;

            return NDIlib.recv_ptz_recall_preset(_recvInstancePtr, index, (float)speed);
        }

        public bool PtzAutoFocus()
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_auto_focus(_recvInstancePtr);
        }

        public bool SetPtzFocusSpeed(double speed)
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_focus_speed(_recvInstancePtr, (float)speed);
        }

        public bool PtzWhiteBalanceAuto()
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_white_balance_auto(_recvInstancePtr);
        }

        public bool PtzWhiteBalanceIndoor()
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_white_balance_indoor(_recvInstancePtr);
        }

        public bool PtzWhiteBalanceOutdoor()
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_white_balance_outdoor(_recvInstancePtr);
        }

        public bool PtzWhiteBalanceOneShot()
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_white_balance_oneshot(_recvInstancePtr);
        }

        public bool PtzWhiteBalanceManual(double red, double blue)
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_white_balance_manual(_recvInstancePtr, (float)red, (float)blue);
        }

        public bool PtzExposureAuto()
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_exposure_auto(_recvInstancePtr);
        }

        public bool PtzExposureManual(double level)
        {
            if (!_isPtz || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_ptz_exposure_manual(_recvInstancePtr, (float)level);
        }

        #endregion

        #region Recording Methods

        public bool RecordingStart(string filenameHint = "")
        {
            if (!_canRecord || _recvInstancePtr == IntPtr.Zero)
                return false;

            if (string.IsNullOrWhiteSpace(filenameHint))
                return NDIlib.recv_recording_start(_recvInstancePtr, IntPtr.Zero);

            IntPtr fileNamePtr = IntPtr.Zero;
            try
            {
                fileNamePtr = NDI.UTF.StringToUtf8(filenameHint);
                return NDIlib.recv_recording_start(_recvInstancePtr, fileNamePtr);
            }
            finally
            {
                if (fileNamePtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(fileNamePtr);
            }
        }

        public bool RecordingStop()
        {
            if (!_canRecord || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_recording_stop(_recvInstancePtr);
        }

        public bool RecordingSetAudioLevel(double level)
        {
            if (!_canRecord || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_recording_set_audio_level(_recvInstancePtr, (float)level);
        }

        public bool IsRecording()
        {
            if (!_canRecord || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_recording_is_recording(_recvInstancePtr);
        }

        public string GetRecordingFilename()
        {
            if (!_canRecord || _recvInstancePtr == IntPtr.Zero)
                return string.Empty;

            IntPtr filenamePtr = NDIlib.recv_recording_get_filename(_recvInstancePtr);
            if (filenamePtr == IntPtr.Zero)
                return string.Empty;

            try
            {
                return NDI.UTF.Utf8ToString(filenamePtr);
            }
            finally
            {
                NDIlib.recv_free_string(_recvInstancePtr, filenamePtr);
            }
        }

        public string GetRecordingError()
        {
            if (!_canRecord || _recvInstancePtr == IntPtr.Zero)
                return string.Empty;

            IntPtr errorPtr = NDIlib.recv_recording_get_error(_recvInstancePtr);
            if (errorPtr == IntPtr.Zero)
                return string.Empty;

            try
            {
                return NDI.UTF.Utf8ToString(errorPtr);
            }
            finally
            {
                NDIlib.recv_free_string(_recvInstancePtr, errorPtr);
            }
        }

        public bool GetRecordingTimes(ref NDIlib.recv_recording_time_t recordingTimes)
        {
            if (!_canRecord || _recvInstancePtr == IntPtr.Zero)
                return false;

            return NDIlib.recv_recording_get_times(_recvInstancePtr, ref recordingTimes);
        }

        #endregion

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
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

                DisposeAudio();
            }

            DestroyReceiver();

            if (_audioScratchHandle.IsAllocated)
                _audioScratchHandle.Free();
        }

        private bool _disposed;

        private static void OnConnectedSourceChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is ReceiveView view)
                view.Connect(view.ConnectedSource);
        }

        private void Connect(Source source)
        {
            int receiverId = Interlocked.Increment(ref _receiverId);

            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            if (Child == null)
                Child = VideoSurface;

            Disconnect();

            if (source == null || string.IsNullOrWhiteSpace(source.Name))
                return;

            if (string.IsNullOrWhiteSpace(ReceiverName))
                throw new ArgumentException("ReceiverName can not be null or empty.", nameof(ReceiverName));

            IntPtr sourceNamePtr = IntPtr.Zero;
            IntPtr recvNamePtr = IntPtr.Zero;

            try
            {
                sourceNamePtr = UTF.StringToUtf8(source.Name);
                recvNamePtr = UTF.StringToUtf8(ReceiverName);

                NDIlib.source_t source_t = new NDIlib.source_t
                {
                    p_ndi_name = sourceNamePtr
                };

                NDIlib.recv_create_v3_t recvDescription = new NDIlib.recv_create_v3_t
                {
                    source_to_connect_to = source_t,
                    color_format = NDIlib.recv_color_format_e.recv_color_format_BGRX_BGRA,
                    bandwidth = IsLowBandwidth
                        ? NDIlib.recv_bandwidth_e.recv_bandwidth_lowest
                        : NDIlib.recv_bandwidth_e.recv_bandwidth_highest,
                    allow_video_fields = false,
                    p_ndi_recv_name = recvNamePtr
                };

                _recvInstancePtr = NDIlib.recv_create_v3(ref recvDescription);
            }
            finally
            {
                if (sourceNamePtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(sourceNamePtr);
                if (recvNamePtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(recvNamePtr);
            }

            System.Diagnostics.Debug.Assert(_recvInstancePtr != IntPtr.Zero, "Failed to create NDI receive instance.");

            if (_recvInstancePtr != IntPtr.Zero)
            {
                SetTallyIndicators(true, false);

                _exitThread = false;
                _receiveThread = new Thread(ReceiveThreadProc)
                {
                    IsBackground = true,
                    Name = "NdiExampleReceiveThread"
                };
                _receiveThread.Start(receiverId);
            }
        }

        public void Disconnect()
        {
            SetTallyIndicators(false, false);

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

            _exitThread = false;

            DisposeAudio();
            DestroyReceiver();

            IsPtz = false;
            IsRecordingSupported = false;
            WebControlUrl = string.Empty;

            Interlocked.Exchange(ref _renderPending, 0);
        }

        private void DestroyReceiver()
        {
            var ptr = _recvInstancePtr;
            _recvInstancePtr = IntPtr.Zero;

            if (ptr != IntPtr.Zero)
            {
                try
                {
                    NDIlib.recv_destroy(ptr);
                }
                catch
                {
                }
            }
        }

        private void DisposeAudio()
        {
            try
            {
                _wasapiOut?.Stop();
            }
            catch
            {
            }

            try
            {
                _wasapiOut?.Dispose();
            }
            catch
            {
            }

            _wasapiOut = null;
            _multiplexProvider = null;
            _bufferedProvider = null;
            _waveFormat = null;
            _haveAudioDevice = true;
        }

        private void SetTallyIndicators(bool onProgram, bool onPreview)
        {
            if (_recvInstancePtr == IntPtr.Zero)
                return;

            NDIlib.tally_t tallyState = new NDIlib.tally_t
            {
                on_program = onProgram,
                on_preview = onPreview
            };

            NDIlib.recv_set_tally(_recvInstancePtr, ref tallyState);
        }

        private void ReceiveThreadProc(object param)
        {
            int currReceiverId = (int)param;

            while (!_exitThread)
            {
                var recvPtr = _recvInstancePtr;
                if (recvPtr == IntPtr.Zero)
                    break;

                NDIlib.video_frame_v2_t videoFrame = new NDIlib.video_frame_v2_t();
                NDIlib.audio_frame_v3_t audioFrame = new NDIlib.audio_frame_v3_t();
                NDIlib.metadata_frame_t metadataFrame = new NDIlib.metadata_frame_t();

                var frameType = NDIlib.recv_capture_v3(recvPtr, ref videoFrame, ref audioFrame, ref metadataFrame, 1000);

                switch (frameType)
                {
                    case NDIlib.frame_type_e.frame_type_none:
                        break;

                    case NDIlib.frame_type_e.frame_type_status_change:
                        HandleStatusChange(recvPtr);
                        break;

                    case NDIlib.frame_type_e.frame_type_video:
                        HandleVideoFrame(currReceiverId, recvPtr, ref videoFrame);
                        break;

                    case NDIlib.frame_type_e.frame_type_audio:
                        HandleAudioFrame(recvPtr, ref audioFrame);
                        break;

                    case NDIlib.frame_type_e.frame_type_metadata:
                        NDIlib.recv_free_metadata(recvPtr, ref metadataFrame);
                        break;
                }
            }
        }

        private void HandleStatusChange(IntPtr recvPtr)
        {
            IsPtz = NDIlib.recv_ptz_is_supported(recvPtr);
            IsRecordingSupported = NDIlib.recv_recording_is_supported(recvPtr);

            IntPtr webUrlPtr = NDIlib.recv_get_web_control(recvPtr);
            if (webUrlPtr == IntPtr.Zero)
            {
                WebControlUrl = string.Empty;
                return;
            }

            try
            {
                WebControlUrl = NDI.UTF.Utf8ToString(webUrlPtr);
            }
            finally
            {
                NDIlib.recv_free_string(recvPtr, webUrlPtr);
            }
        }

        private void HandleVideoFrame(int currReceiverId, IntPtr recvPtr, ref NDIlib.video_frame_v2_t videoFrame)
        {
            if (!_videoEnabled || videoFrame.p_data == IntPtr.Zero)
            {
                NDIlib.recv_free_video_v2(recvPtr, ref videoFrame);
                return;
            }

            if (Interlocked.Exchange(ref _renderPending, 1) == 1)
            {
                NDIlib.recv_free_video_v2(recvPtr, ref videoFrame);
                return;
            }

            // копируем struct в локальную переменную
            var frame = videoFrame;

            int yres = (int)frame.yres;
            int xres = (int)frame.xres;
            int stride = (int)frame.line_stride_in_bytes;
            int bufferSize = yres * stride;
            double dpiX = 96.0 * (frame.picture_aspect_ratio / ((double)xres / yres));

            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (currReceiverId != _receiverId || _receiverId == 0 || _disposed)
                        return;

                    if (VideoBitmap == null ||
                        VideoBitmap.PixelWidth != xres ||
                        VideoBitmap.PixelHeight != yres ||
                        Math.Abs(VideoBitmap.DpiX - dpiX) > 0.001)
                    {
                        VideoBitmap = new WriteableBitmap(xres, yres, dpiX, 96.0, PixelFormats.Bgra32, null);
                        VideoSurface.Source = VideoBitmap;
                    }

                    VideoBitmap.WritePixels(
                        new Int32Rect(0, 0, xres, yres),
                        frame.p_data,
                        bufferSize,
                        stride);
                }
                finally
                {
                    try
                    {
                        if (recvPtr != IntPtr.Zero)
                            NDIlib.recv_free_video_v2(recvPtr, ref frame);
                    }
                    catch
                    {
                    }

                    Interlocked.Exchange(ref _renderPending, 0);
                }
            }), DispatcherPriority.Render);
        }

        private void HandleAudioFrame(IntPtr recvPtr, ref NDIlib.audio_frame_v3_t audioFrame)
        {
            try
            {
                if (!_audioEnabled || audioFrame.p_data == IntPtr.Zero || audioFrame.no_samples == 0)
                    return;

                bool formatChanged = false;

                if (_waveFormat == null ||
                    _waveFormat.Channels != audioFrame.no_channels ||
                    _waveFormat.SampleRate != audioFrame.sample_rate)
                {
                    _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(
                        (int)audioFrame.sample_rate,
                        (int)audioFrame.no_channels);

                    formatChanged = true;
                }

                if (_bufferedProvider == null || formatChanged)
                {
                    _bufferedProvider = new BufferedWaveProvider(_waveFormat)
                    {
                        DiscardOnBufferOverflow = true,
                        BufferDuration = TimeSpan.FromMilliseconds(200)
                    };
                }

                if (_multiplexProvider == null || formatChanged)
                {
                    _multiplexProvider = new MultiplexingWaveProvider(
                        new List<IWaveProvider> { _bufferedProvider }, 2);

                    if (_waveFormat.Channels == 1)
                    {
                        _multiplexProvider.ConnectInputToOutput(0, 0);
                        _multiplexProvider.ConnectInputToOutput(0, 1);
                    }
                    else
                    {
                        _multiplexProvider.ConnectInputToOutput(0, 0);
                        _multiplexProvider.ConnectInputToOutput(1, 1);
                    }
                }

                if (_haveAudioDevice && (_wasapiOut == null || formatChanged))
                {
                    try
                    {
                        _wasapiOut?.Stop();
                        _wasapiOut?.Dispose();
                        _wasapiOut = null;

                        _wasapiOut = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, true, 50);
                        _wasapiOut.Init(_multiplexProvider);
                        _wasapiOut.Volume = _volume;
                        _wasapiOut.Play();
                    }
                    catch
                    {
                        _haveAudioDevice = false;
                    }
                }

                if (_haveAudioDevice && _wasapiOut != null)
                {
                    int sizeInBytes = (int)audioFrame.no_samples * (int)audioFrame.no_channels * sizeof(float);
                    EnsureAudioScratch(sizeInBytes);

                    NDIlib.audio_frame_interleaved_32f_t interleavedFrame = new NDIlib.audio_frame_interleaved_32f_t
                    {
                        sample_rate = audioFrame.sample_rate,
                        no_channels = audioFrame.no_channels,
                        no_samples = audioFrame.no_samples,
                        timecode = audioFrame.timecode,
                        p_data = _audioScratchHandle.AddrOfPinnedObject()
                    };

                    NDIlib.util_audio_to_interleaved_32f_v3(ref audioFrame, ref interleavedFrame);
                    _bufferedProvider.AddSamples(_audioScratch, 0, sizeInBytes);
                }
            }
            finally
            {
                NDIlib.recv_free_audio_v3(recvPtr, ref audioFrame);
            }
        }

        private void EnsureAudioScratch(int sizeInBytes)
        {
            if (_audioScratch != null && _audioScratchSize >= sizeInBytes)
                return;

            if (_audioScratchHandle.IsAllocated)
                _audioScratchHandle.Free();

            _audioScratch = new byte[sizeInBytes];
            _audioScratchHandle = GCHandle.Alloc(_audioScratch, GCHandleType.Pinned);
            _audioScratchSize = sizeInBytes;
        }

        // NDI receiver
        private IntPtr _recvInstancePtr = IntPtr.Zero;

        // receive thread
        private Thread _receiveThread;
        private volatile bool _exitThread;

        // video
        private readonly Image VideoSurface = new Image();
        private WriteableBitmap VideoBitmap;
        private int _renderPending = 0;

        // audio
        private bool _audioEnabled = true;
        private bool _videoEnabled = true;
        private WasapiOut _wasapiOut;
        private bool _haveAudioDevice = true;
        private MultiplexingWaveProvider _multiplexProvider;
        private BufferedWaveProvider _bufferedProvider;
        private WaveFormat _waveFormat;
        private float _volume = 1.0f;

        // reusable audio buffer
        private byte[] _audioScratch;
        private GCHandle _audioScratchHandle;
        private int _audioScratchSize;

        // capabilities/state
        private bool _isPtz;
        private bool _canRecord;
        private string _webControlUrl = string.Empty;

        // receiver generation
        private int _receiverId = 0;
    }
}