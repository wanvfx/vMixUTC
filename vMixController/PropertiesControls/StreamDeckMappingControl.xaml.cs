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

        /// <summary>
        /// Gets the RemoveControlCommand.
        /// </summary>
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
                        Keys.Add(new Widgets.StreamDeckKey() { A = "" });//Sanford.Multimedia.Midi.ChannelCommand.Controller });
                    }));
            }
        }


        private RelayCommand<Widgets.StreamDeckKey> _learnStreamDeckKey;

        /// <summary>
        /// Gets the LearnMidiKey.
        /// </summary>
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
                            //p.B = result.B;
                            //p.D = result.D;
                        }
                    }));
            }
        }

        public StreamDeckMappingControl()
        {
            InitializeComponent();
            //DataContext = this;
        }
    }
}
