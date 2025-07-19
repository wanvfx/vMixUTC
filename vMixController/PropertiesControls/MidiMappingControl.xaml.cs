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
    public partial class MidiMappingControl : UserControl, INotifyPropertyChanged
    {

        public static readonly DependencyProperty LearnFunctionProperty =
    DependencyProperty.Register(
        nameof(LearnFunction),
        typeof(Func<Widgets.MidiInterfaceKey>),
        typeof(MidiMappingControl),
        new PropertyMetadata(null));

        public Func<Widgets.MidiInterfaceKey> LearnFunction
        {
            get => (Func<Widgets.MidiInterfaceKey>)GetValue(LearnFunctionProperty);
            set => SetValue(LearnFunctionProperty, value);
        }

        public static readonly DependencyProperty MidisProperty =
    DependencyProperty.Register(
        nameof(Midis),
        typeof(ObservableCollection<Widgets.MidiInterfaceKey>),
        typeof(MidiMappingControl),
        new PropertyMetadata(new ObservableCollection<Widgets.MidiInterfaceKey>()));

        public ObservableCollection<Widgets.MidiInterfaceKey> Midis
        {
            get => (ObservableCollection<Widgets.MidiInterfaceKey>)GetValue(MidisProperty);
            set => SetValue(MidisProperty, value);
        }

        private RelayCommand<Widgets.MidiInterfaceKey> _removePathCommand;

        public RelayCommand<Widgets.MidiInterfaceKey> RemovePathCommand
        {
            get
            {
                return _removePathCommand
                    ?? (_removePathCommand = new RelayCommand<Widgets.MidiInterfaceKey>(
                    p =>
                    {
                        Midis.Remove(p);
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
                        Midis.Add(new Widgets.MidiInterfaceKey() { A = -1, B = -1, C = "", D = Melanchall.DryWetMidi.Core.MidiEventType.ControlChange });
                    }));
            }
        }


        private RelayCommand<Widgets.MidiInterfaceKey> _learnmidiKey;

        public RelayCommand<Widgets.MidiInterfaceKey> LearnMidiKey
        {
            get
            {
                return _learnmidiKey
                    ?? (_learnmidiKey = new RelayCommand<Widgets.MidiInterfaceKey>(
                    p =>
                    {
                        var result = LearnFunction?.Invoke();
                        if (result != null)
                        {
                            p.A = result.A;
                            p.B = result.B;
                            p.D = result.D;
                        }
                    }));
            }
        }

        public MidiMappingControl()
        {
            InitializeComponent();
        }
    }
}