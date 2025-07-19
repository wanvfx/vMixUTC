using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace vMixController.PropertiesControls
{
    /// <summary>
    /// Логика взаимодействия для PathsControl.xaml
    /// </summary>
    public partial class StreamDeckMappingControl : UserControl, INotifyPropertyChanged
    {

        public static readonly DependencyProperty LearnFunctionProperty =
    DependencyProperty.Register(
        nameof(LearnFunction),
        typeof(Func<Widgets.StreamDeckKey>),
        typeof(StreamDeckMappingControl),
        new PropertyMetadata(null));

        public Func<Widgets.StreamDeckKey> LearnFunction
        {
            get => (Func<Widgets.StreamDeckKey>)GetValue(LearnFunctionProperty);
            set => SetValue(LearnFunctionProperty, value);
        }

        public static readonly DependencyProperty KeysProperty =
    DependencyProperty.Register(
        nameof(Keys),
        typeof(ObservableCollection<Widgets.StreamDeckKey>),
        typeof(StreamDeckMappingControl),
        new PropertyMetadata(new ObservableCollection<Widgets.StreamDeckKey>()));

        public ObservableCollection<Widgets.StreamDeckKey> Keys
        {
            get => (ObservableCollection<Widgets.StreamDeckKey>)GetValue(KeysProperty);
            set => SetValue(KeysProperty, value);
        }

        private RelayCommand<Widgets.StreamDeckKey> _removePathCommand;

        public RelayCommand<Widgets.StreamDeckKey> RemovePathCommand
        {
            get
            {
                return _removePathCommand
                    ?? (_removePathCommand = new RelayCommand<Widgets.StreamDeckKey>(
                    p =>
                    {
                        Keys.Remove(p);
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
                        Keys.Add(new Widgets.StreamDeckKey() { A = "" });
                    }));
            }
        }


        private RelayCommand<Widgets.StreamDeckKey> _learnStreamDeckKey;

        public RelayCommand<Widgets.StreamDeckKey> LearnStreamDeckKey
        {
            get
            {
                return _learnStreamDeckKey
                    ?? (_learnStreamDeckKey = new RelayCommand<Widgets.StreamDeckKey>(
                    p =>
                    {
                        var result = LearnFunction?.Invoke();
                        if (result != null)
                        {
                            p.A = result.A;
                        }
                    }));
            }
        }

        public StreamDeckMappingControl()
        {
            InitializeComponent();
        }
    }
}