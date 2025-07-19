using GalaSoft.MvvmLight.CommandWpf;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using vMixController.Classes;
using vMixController.Interfaces;

namespace vMixController.PropertiesControls
{
    /// <summary>
    /// Логика взаимодействия для PathsControl.xaml
    /// </summary>
    public partial class TitleMappingControl : UserControl, INotifyPropertyChanged, ICancellable
    {

        public static readonly DependencyProperty TitlesProperty =
            DependencyProperty.Register(
                nameof(Titles),
                typeof(ObservableCollection<Pair<string, string>>),
                typeof(TitleMappingControl),
                new PropertyMetadata(null));

        public ObservableCollection<Pair<string, string>> Titles
        {
            get { return (ObservableCollection<Pair<string, string>>)GetValue(TitlesProperty); }
            set { SetValue(TitlesProperty, value); }
        }

        public static readonly DependencyProperty IsCancelledPropertyName =
            DependencyProperty.Register(nameof(IsCancelled), typeof(bool), typeof(TitleMappingControl), new PropertyMetadata(false));

        public bool IsCancelled
        {
            get { return (bool)GetValue(IsCancelledPropertyName); }
            set { SetValue(IsCancelledPropertyName, value); }
        }

        private RelayCommand<Pair<string, string>> _removePathCommand;

        public RelayCommand<Pair<string, string>> RemovePathCommand
        {
            get
            {
                return _removePathCommand
                    ?? (_removePathCommand = new RelayCommand<Pair<string, string>>(
                    p =>
                    {
                        Titles.Remove(p);
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
                        Titles.Add(new Pair<string, string>() { A = null, B = "" });
                    }));
            }
        }

        public TitleMappingControl()
        {
            InitializeComponent();
        }
    }
}