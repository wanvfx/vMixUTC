using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using vMixController.Classes;

namespace vMixController
{
    /// <summary>
    /// Логика взаимодействия для MidiLearnWindow.xaml
    /// </summary>
    public partial class StreamDeckLearnWindow : Window
    {
        private bool _doNotDispose;
        private StreamDeckConnector _device = null;

        public Widgets.StreamDeckKey Key { get; set; }

        public StreamDeckLearnWindow()
        {
            InitializeComponent();
            Topmost = Application.Current?.MainWindow?.Topmost ?? false;
            Activate();
        }

        public StreamDeckLearnWindow(StreamDeckConnector device, bool doNotDispose = true)
        {
            InitializeComponent();
            _doNotDispose = doNotDispose;
            if (device != null)
            {
                _device = device;
                _device.OnStreamDeckEvent += _device_OnStreamDeckEvent; ;
                //_device.ChannelMessageReceived += Device_ChannelMessageReceived;
                //_device.Reset();

            }
        }

        private void _device_OnStreamDeckEvent(object sender, StreamDeckEvent e)
        {
            //if (e.Button != null)
            {
                Dispatcher.Invoke(() => Key = new Widgets.StreamDeckKey() { A = e.Context });
                try
                {
                    Dispatcher.Invoke(() => DialogResult = true);
                }
                catch (Exception) { }
                //Debug.WriteLine(e.Message);
            }
        }
        

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            DialogResult = false;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_device != null)
            {
                //_device.ChannelMessageReceived -= Device_ChannelMessageReceived;
                _device.OnStreamDeckEvent -= _device_OnStreamDeckEvent;
                if (!_doNotDispose)
                {
                    _device.Dispose();
                }
            }
        }
    }
}
