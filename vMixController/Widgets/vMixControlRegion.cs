using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Windows.Controls;

namespace vMixController.Widgets
{
    public class vMixControlRegion: vMixControl
    {
        public vMixControlRegion()
        {
            ZIndex = -1;
        }

        public override string Type
        {
            get
            {
                return "Region";
            }
        }

        public override bool IsResizeableVertical => true;

        private string _Text = "";

        /// <summary>
        /// Sets and gets the Text property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Text
        {
            get
            {
                return _Text;
            }

            set
            {
                if (_Text == value)
                {
                    return;
                }

                _Text = value;
                RaisePropertyChanged(nameof(Text));
            }
        }

        private bool _sticky = false;

        /// <summary>
        /// Sets and gets the Magnet property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool Sticky
        {
            get
            {
                return _sticky;
            }

            set
            {
                if (_sticky == value)
                {
                    return;
                }

                _sticky = value;
                RaisePropertyChanged(nameof(Sticky));
            }
        }

        private bool _isEditable = false;

        /// <summary>
        /// Sets and gets the Magnet property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsEditable
        {
            get
            {
                return _isEditable;
            }

            set
            {
                if (_isEditable == value)
                {
                    return;
                }

                _isEditable = value;
                RaisePropertyChanged(nameof(IsEditable));
            }
        }

        [NonSerialized]
        private RelayCommand<object> _mouseDoubleClick;

        /// <summary>
        /// Gets the ExecutePushOn.
        /// </summary>
        public RelayCommand<object> MouseDoubleClick
        {
            get
            {
                return _mouseDoubleClick
                    ?? (_mouseDoubleClick = new RelayCommand<object>(
                    (p) =>
                    {
                        //MouseEventArgs

                        IsEditable = true;
                        //p.Handled = true;

                    }));
            }
        }

        public override void BeforePropertiesChanged()
        {
            base.BeforePropertiesChanged();
        }

        public override void AfterPropertiesChanged()
        {
            base.AfterPropertiesChanged();
        }

        public override void Update()
        {
            Height++;
            Height--;
            base.Update();
        }
    }
}
