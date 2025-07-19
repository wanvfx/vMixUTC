using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Linq;
using System.Windows.Controls;
using System.Xml.Serialization;
using vMixAPI;
using vMixController.Classes;

namespace vMixController.Widgets
{
    [Serializable]
    public class vMixControlMultiState : vMixControl
    {

        public string IP { get; set; }
        public string Port { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }

        [XmlIgnore]
        public State DummyState { get; set; }


        /// <summary>
        /// The <see cref="Enabled" /> property's name.
        /// </summary>
        public const string EnabledPropertyName = "Enabled";

        private bool _enabled = true;

        /// <summary>
        /// Sets and gets the Enabled property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool Enabled
        {
            get
            {
                return _enabled;
            }

            set
            {
                if (_enabled == value)
                {
                    return;
                }

                _enabled = value;
                RaisePropertyChanged(EnabledPropertyName);
            }
        }

        public override string Type
        {
            get
            {
                return Extensions.LocalizationManager.Get("Multi State");
            }
        }

        [XmlIgnore]
        public override State State
        {
            get
            {
                return base.State;
            }

            set
            {
                if (base.State != null)
                    base.State.OnFunctionSend -= State_OnFunctionSend;
                base.State = value;
                if (base.State != null)
                    base.State.OnFunctionSend += State_OnFunctionSend;
            }
        }

        private void State_OnFunctionSend(object sender, FunctionSendArgs e)
        {
            if (DummyState == null)
                DummyState = new State();
            DummyState.Configure(IP, Port, Login, Password);
            if (Enabled)
                DummyState.SendFunction(e.Function);
        }

        public override UserControl[] GetPropertiesControls()
        {
            return base.GetPropertiesControls();
        }

        public override void SetProperties(UserControl[] _controls)
        {
            base.SetProperties(_controls);
        }

        public override Hotkey[] GetHotkeys()
        {

            return base.GetHotkeys().Concat(new Hotkey[] { new Hotkey() { Name = "Toggle\nEnabled" } }).ToArray();
        }

        public override void ExecuteHotkey(int index)
        {
            if (index == 0)
                Enabled = !Enabled;
        }

        public vMixControlMultiState()
        {
            IP = "127.0.0.1";
            Port = "8088";
            Login = "admin";
            Password = "";
        }

        [NonSerialized]
        private RelayCommand _toggleEnabledCommand;

        /// <summary>
        /// Gets the ToggleEnabled.
        /// </summary>
        public RelayCommand ToggleEnabledCommand
        {
            get
            {
                return _toggleEnabledCommand
                    ?? (_toggleEnabledCommand = new RelayCommand(
                    () =>
                    {
                        Enabled = !Enabled;
                    }));
            }
        }
    }
}