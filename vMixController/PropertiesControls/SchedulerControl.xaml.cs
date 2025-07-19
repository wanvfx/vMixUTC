using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using vMixController.Classes;

namespace vMixController.PropertiesControls
{
    /// <summary>
    /// Логика взаимодействия для PathsControl.xaml
    /// </summary>
    public partial class SchedulerControl : UserControl, INotifyPropertyChanged
    {

        public static readonly DependencyProperty EventsProperty =
            DependencyProperty.Register(
                nameof(Events),
                typeof(ObservableCollection<ScheduledEvent>),
                typeof(SchedulerControl),
                new PropertyMetadata(new ObservableCollection<ScheduledEvent>()));

        public ObservableCollection<ScheduledEvent> Events
        {
            get => (ObservableCollection<ScheduledEvent>)GetValue(EventsProperty);
            set => SetValue(EventsProperty, value);
        }

        private RelayCommand<ScheduledEvent> _removePathCommand;

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

        public RelayCommand<ScheduledEvent> CheckTH
        {
            get
            {
                return _checkTH
                    ?? (_checkTH = new RelayCommand<ScheduledEvent>(
                    p =>
                    {
                        p.Days = p.Days ^ DaysOfWeek.Thursday;
                    }));
            }
        }

        private RelayCommand<ScheduledEvent> _checkF;

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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

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
        }
    }
}