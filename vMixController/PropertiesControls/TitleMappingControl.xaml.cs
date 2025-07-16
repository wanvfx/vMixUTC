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
using vMixController.Interfaces;

namespace vMixController.PropertiesControls
{
    /// <summary>
    /// Логика взаимодействия для PathsControl.xaml
    /// </summary>
    public partial class TitleMappingControl : UserControl, INotifyPropertyChanged, ICancellable
    {

        /// <summary>
        /// Регистрация DependencyProperty. 
        /// Это статическое поле хранит метаданные свойства.
        /// </summary>
        public static readonly DependencyProperty TitlesProperty =
            DependencyProperty.Register(
                nameof(Titles),
                typeof(ObservableCollection<Pair<string, string>>),
                typeof(TitleMappingControl), // <-- ЗАМЕНИТЕ MyUserControl НА ИМЯ ВАШЕГО КЛАССА
                new PropertyMetadata(null));

        /// <summary>
        /// CLR-обертка для доступа к свойству из кода.
        /// Система WPF будет вызывать GetValue/SetValue напрямую,
        /// а вы можете использовать эту обертку для удобства.
        /// </summary>
        public ObservableCollection<Pair<string, string>> Titles
        {
            get { return (ObservableCollection<Pair<string, string>>)GetValue(TitlesProperty); }
            set { SetValue(TitlesProperty, value); }
        }


        /// <summary>
        /// The <see cref="IsCancelled" /> property's name.
        /// </summary>
        public const string IsCancelledPropertyName = "IsCancelled";

        private bool _isCancelled = false;

        /// <summary>
        /// Sets and gets the IsCancelled property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsCancelled
        {
            get
            {
                return _isCancelled;
            }

            set
            {
                if (_isCancelled == value)
                {
                    return;
                }

                _isCancelled = value;

                RaisePropertyChanged(IsCancelledPropertyName);
            }
        }

        private RelayCommand<Pair<string, string>> _removePathCommand;

        /// <summary>
        /// Gets the RemoveControlCommand.
        /// </summary>
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
                        Titles.Add(new Pair<string, string>() { A = null, B = "" });
                    }));
            }
        }

        /*/// <summary>
        /// The <see cref="IsGUIDTargeted" /> property's name.
        /// </summary>
        public const string IsGUIDTargetedPropertyName = "IsGUIDTargeted";

        private bool _isGUIDTargeted = true;

        /// <summary>
        /// Sets and gets the IsGUIDTargeted property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsGUIDTargeted
        {
            get
            {
                return _isGUIDTargeted;
            }

            set
            {
                if (_isGUIDTargeted == value)
                {
                    return; 
                }

                _isGUIDTargeted = value;
                RaisePropertyChanged(IsGUIDTargetedPropertyName);
            }
        }*/

        public TitleMappingControl()
        {
            InitializeComponent();
            //DataContext = this;
        }
    }
}
