using GalaSoft.MvvmLight.CommandWpf;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using vMixController.Classes;

namespace vMixController.PropertiesControls
{
    /// <summary>
    /// Логика взаимодействия для ListControl.xaml
    /// </summary>
    public partial class ListControl : UserControl, INotifyPropertyChanged
    {
        public ListControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(
                nameof(Items),
                typeof(ObservableCollection<DummyStringProperty>),
                typeof(ListControl),
                new PropertyMetadata(null));

        public ObservableCollection<DummyStringProperty> Items
        {
            get { return (ObservableCollection<DummyStringProperty>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        private RelayCommand _addItemCommand;

        public RelayCommand AddItemCommand
        {
            get
            {
                return _addItemCommand
                    ?? (_addItemCommand = new RelayCommand(
                    () =>
                    {
                        Items.Add(new DummyStringProperty() { Value = "" });
                    }));
            }
        }

        private RelayCommand<DummyStringProperty> _removeItemCommand;

        public RelayCommand<DummyStringProperty> RemoveItemCommand
        {
            get
            {
                return _removeItemCommand
                    ?? (_removeItemCommand = new RelayCommand<DummyStringProperty>(
                    p =>
                    {
                        Items.Remove(p);
                    }));
            }
        }

        private RelayCommand _saveItemsListCommand;

        public RelayCommand SaveItemsListCommand
        {
            get
            {
                return _saveItemsListCommand
                    ?? (_saveItemsListCommand = new RelayCommand(
                    () =>
                    {
                        Ookii.Dialogs.Wpf.VistaSaveFileDialog opendlg = new Ookii.Dialogs.Wpf.VistaSaveFileDialog
                        {
                            Filter = "Text Files|*.txt",
                            DefaultExt = "txt"
                        };
                        var result = opendlg.ShowDialog(App.Current.Windows.OfType<vMixWidgetSettingsView>().FirstOrDefault());
                        if (result.HasValue && result.Value)
                            File.WriteAllLines(opendlg.FileName, Items.Select(x => x.Value).ToArray());
                    }));
            }
        }

        private RelayCommand _loadItemsListCommand;

        public event PropertyChangedEventHandler PropertyChanged;

        internal void RaisePropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public RelayCommand LoadItemsListCommand
        {
            get
            {
                return _loadItemsListCommand
                    ?? (_loadItemsListCommand = new RelayCommand(
                    () =>
                    {
                        Ookii.Dialogs.Wpf.VistaOpenFileDialog opendlg = new Ookii.Dialogs.Wpf.VistaOpenFileDialog
                        {
                            Filter = "Text Files|*.txt",
                            DefaultExt = "txt"
                        };
                        var result = opendlg.ShowDialog(App.Current.Windows.OfType<vMixWidgetSettingsView>().FirstOrDefault());
                        if (result.HasValue && result.Value)
                        {
                            Items.Clear();
                            foreach (var item in File.ReadAllLines(opendlg.FileName)/*.OrderBy(x => x)*/)
                            {
                                Items.Add(new DummyStringProperty() { Value = item });
                            }
                        }
                    }));
            }
        }

    }
}