using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using vMixController.Classes;
using vMixController.Widgets;

namespace vMixController.PropertiesControls
{
    /// <summary>
    /// Логика взаимодействия для PathsControl.xaml
    /// </summary>
    public partial class SchedulerControl : UserControl, INotifyPropertyChanged
    {




        /// <summary>
            /// The <see cref="Events" /> property's name.
            /// </summary>
        public const string EventsPropertyName = "Events";

        private ObservableCollection<ScheduledEvent> _events = new ObservableCollection<ScheduledEvent> ();

        /// <summary>
        /// Sets and gets the Events property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<ScheduledEvent> Events
        {
            get
            {
                return _events;
            }

            set
            {
                if (_events == value)
                {
                    return;
                }

                _events = value;
                RaisePropertyChanged(EventsPropertyName);
            }
        }

        private RelayCommand<ScheduledEvent> _removePathCommand;

        /// <summary>
        /// Gets the RemoveControlCommand.
        /// </summary>
        public RelayCommand<ScheduledEvent> RemovePathCommand
        {
            get
            {
                return _removePathCommand
                    ?? (_removePathCommand = new RelayCommand<ScheduledEvent>(
                    p =>
                    {
                        Events.Remove(p);
                    }));
            }
        }

        private RelayCommand<ScheduledEvent> _checkM;

        /// <summary>
        /// Gets the CheckM.
        /// </summary>
        public RelayCommand<ScheduledEvent> CheckM
        {
            get
            {
                return _checkM
                    ?? (_checkM = new RelayCommand<ScheduledEvent>(
                    p =>
                    {
                        p.Days = p.Days ^ DaysOfWeek.Monday;
                    }));
            }
        }

        private RelayCommand<ScheduledEvent> _checkT;

        /// <summary>
        /// Gets the CheckM.
        /// </summary>
        public RelayCommand<ScheduledEvent> CheckT
        {
            get
            {
                return _checkT
                    ?? (_checkT = new RelayCommand<ScheduledEvent>(
                    p =>
                    {
                        p.Days = p.Days ^ DaysOfWeek.Tuesday;
                    }));
            }
        }

        private RelayCommand<ScheduledEvent> _checkW;

        /// <summary>
        /// Gets the CheckM.
        /// </summary>
        public RelayCommand<ScheduledEvent> CheckW
        {
            get
            {
                return _checkW
                    ?? (_checkW = new RelayCommand<ScheduledEvent>(
                    p =>
                    {
                        p.Days = p.Days ^ DaysOfWeek.Wednesday;
                    }));
            }
        }

        private RelayCommand<ScheduledEvent> _checkTH;

        /// <summary>
        /// Gets the CheckM.
        /// </summary>
        public RelayCommand<ScheduledEvent> CheckTH
        {
            get
            {
                return _checkTH
                    ?? (_checkTH = new RelayCommand<ScheduledEvent>(
                    p =>
                    {
                        p.Days = p.Days ^ DaysOfWeek.Tuesday;
                    }));
            }
        }

        private RelayCommand<ScheduledEvent> _checkF;

        /// <summary>
        /// Gets the CheckM.
        /// </summary>
        public RelayCommand<ScheduledEvent> CheckF
        {
            get
            {
                return _checkF
                    ?? (_checkF = new RelayCommand<ScheduledEvent>(
                    p =>
                    {
                        p.Days = p.Days ^ DaysOfWeek.Friday;
                    }));
            }
        }

        private RelayCommand<ScheduledEvent> _checkS;

        /// <summary>
        /// Gets the CheckM.
        /// </summary>
        public RelayCommand<ScheduledEvent> CheckS
        {
            get
            {
                return _checkS
                    ?? (_checkS = new RelayCommand<ScheduledEvent>(
                    p =>
                    {
                        p.Days = p.Days ^ DaysOfWeek.Saturday;
                    }));
            }
        }

        private RelayCommand<ScheduledEvent> _checkSU;

        /// <summary>
        /// Gets the CheckM.
        /// </summary>
        public RelayCommand<ScheduledEvent> CheckSU
        {
            get
            {
                return _checkSU
                    ?? (_checkSU = new RelayCommand<ScheduledEvent>(
                    p =>
                    {
                        p.Days = p.Days ^ DaysOfWeek.Sunday;
                    }));
            }
        }

        private RelayCommand _addPathCommand;

        public event PropertyChangedEventHandler PropertyChanged;
        internal void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        /// <summary>
        /// Gets the AddPathCommand.
        /// </summary>
        public RelayCommand AddPathCommand
        {
            get
            {
                return _addPathCommand
                    ?? (_addPathCommand = new RelayCommand(
                    () =>
                    {
                        var now = DateTime.Now;
                        Events.Add(new ScheduledEvent() { TimeOfDay = DateTime.Now, Days = DaysOfWeek.Everyday });
                    }));
            }
        }

        public SchedulerControl()
        {
            InitializeComponent();
            //DataContext = this;
        }
    }
}
