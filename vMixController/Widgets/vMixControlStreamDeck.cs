using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using Sanford.Multimedia;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml.Serialization;
using vMixController.Classes;
using vMixController.PropertiesControls;

namespace vMixController.Widgets
{
    public class vMixControlStreamDeck : vMixControl
    {
        public override int MaxCount => 1;
        /// <summary>
        /// The <see cref="Context" /> property's name.
        /// </summary>
        public const string ContextPropertyName = "Context";

        private string _context = "[NO BUTTON PRESSED]";

        /// <summary>
        /// Sets and gets the DeviceCaps property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Context
        {
            get
            {
                return _context;
            }

            set
            {
                if (_context == value)
                {
                    return;
                }

                _context = value;
                RaisePropertyChanged(ContextPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="Keys" /> property's name.
        /// </summary>
        public const string KeysPropertyName = "Keys";

        private ObservableCollection<StreamDeckKey> _keys = new ObservableCollection<StreamDeckKey>();

        /// <summary>
        /// Sets and gets the Midis property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<StreamDeckKey> Keys
        {
            get
            {
                return _keys;
            }

            set
            {
                if (_keys == value)
                {
                    return;
                }

                _keys = value;
                RaisePropertyChanged(KeysPropertyName);
            }
        }

        //private static vMixStreamDeckLibrary.StreamDeckAPI device = null;
        private static StreamDeckConnector _connector = new StreamDeckConnector();

        public override string Type
        {
            get
            {
                return "Stream Deck Device";
            }
        }

        public vMixControlStreamDeck()
        {



            _connector.OnStreamDeckEvent += _connector_OnStreamDeckEvent;
            
            //if (device == null)
            //   device = new vMixStreamDeckLibrary.StreamDeckAPI();
            //device.EventReceived += Device_EventReceived;
            //device.Dispose();
        }

        private void _connector_OnStreamDeckEvent(object sender, StreamDeckEvent e)
        {
            Dispatcher.Invoke(() =>
            {
                //A - context
                //B - execLink
                //if (e.Button != null)
                {
                    foreach (var item in Keys)
                    {
                        if (item.A == e.Context && e.Type == vMixStreamDeckLibrary.StreamDeckEvent.KeyUp)
                            Messenger.Default.Send(new Pair<string, object>(item.B, null));
                    }
                    Context = e.Context.ToUpper();
                }
                //Debug.WriteLine(e.Message);
            });
        }

        [NonSerialized]
        private RelayCommand _resetCommand;

        /// <summary>
        /// Gets the ExecuteScriptCommand.
        /// </summary>
        [XmlIgnore]
        public RelayCommand ResetCommand
        {
            get
            {
                return _resetCommand
                    ?? (_resetCommand = new RelayCommand(
                    () =>
                    {
                        //device.Dispose();
                        //device = new vMixStreamDeckLibrary.StreamDeckAPI();
                        //device.EventReceived += Device_EventReceived;

                    }));
            }
        }

        private StreamDeckKey Learn()
        {

            var wnd = new StreamDeckLearnWindow(_connector);
            var result = wnd.ShowDialog();
            if (result ?? true)
            {
                var k = wnd.Key;
                wnd.Close();
                return k;
            }

            return null;
            //throw new NotImplementedException();
        }

        public override UserControl[] GetPropertiesControls()
        {
            var midiMappingCtrl = GetPropertyControl<StreamDeckMappingControl>();
            midiMappingCtrl.LearnFunction = Learn;


            midiMappingCtrl.Keys.Clear();
            foreach (var item in Keys)
            {
                midiMappingCtrl.Keys.Add(item);
            }
            return base.GetPropertiesControls().Union(new UserControl[] { midiMappingCtrl }).ToArray();
        }

        public override void SetProperties(UserControl[] _controls)
        {
            var ctrl = _controls.OfType<StreamDeckMappingControl>().First();
            Keys.Clear();
            foreach (var item in ctrl.Keys)
            {
                Keys.Add(item);
            }
            base.SetProperties(_controls);
        }

        public override void Dispose()
        {
            /*if (device != null)
            {
                device.Dispose();
                device = null;
            }*/
            base.Dispose();
        }

        protected override void Dispose(bool managed)
        {
            base.Dispose(managed);
        }

    }

    public class StreamDeckKey : Quadriple<string, string, int, int>
    {

    }
}
