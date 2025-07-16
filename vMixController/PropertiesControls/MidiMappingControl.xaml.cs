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

        /// <summary>
        /// Gets the RemoveControlCommand.
        /// </summary>
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
                        Midis.Add(new Widgets.MidiInterfaceKey() { A = -1, B = -1, C = "", D = Melanchall.DryWetMidi.Core.MidiEventType.ControlChange });//Sanford.Multimedia.Midi.ChannelCommand.Controller });
                    }));
            }
        }


        private RelayCommand<Widgets.MidiInterfaceKey> _learnmidiKey;

        /// <summary>
        /// Gets the LearnMidiKey.
        /// </summary>
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
            //DataContext = this;
        }
    }
}
