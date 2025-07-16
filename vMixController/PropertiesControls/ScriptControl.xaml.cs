using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;
using vMixAPI;
using vMixController.Classes;
using vMixController.Interfaces;
using vMixController.Widgets;

namespace vMixController.PropertiesControls
{
    /// <summary>
    /// Логика взаимодействия для ScriptControl.xaml
    /// </summary>
    public partial class ScriptControl : UserControl, INotifyPropertyChanged, ICancellable
    {
        public ScriptControl()
        {
            InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                Commands = new ObservableCollection<vMixControlButtonCommand>();
                Commands.CollectionChanged += OnCommandsChanged;
                Commands.Add(new vMixControlButtonCommand());
            }


        }

        private void OnCommandsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //Re-generate code, when any of properties was changed
            if (e.NewItems != null)
                foreach (vMixControlButtonCommand cmd in e.NewItems)
                {
                    cmd.PropertyChanged += OnCommandPropertyChanged;
                    foreach (One<string> par in cmd.AdditionalParameters)
                        par.PropertyChanged += OnAdditionalParameterChanged;
                }
            if (e.OldItems != null)
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset || e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                    foreach (vMixControlButtonCommand cmd in e.OldItems)
                    {
                        cmd.PropertyChanged -= OnCommandPropertyChanged;
                        foreach (One<string> par in cmd.AdditionalParameters)
                            par.PropertyChanged -= OnAdditionalParameterChanged;
                    }
            RearrangeCommnads();
            GenerateCode();
        }

        private void OnAdditionalParameterChanged(object sender, PropertyChangedEventArgs e)
        {
            GenerateCode();
        }

        private void OnCommandPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            GenerateCode();
        }

        private void GenerateCode()
        {
            if (Commands.Count > 0)
                TextCode = Commands.Select(x=>x.ToString()).Aggregate((x, y) => x + "\r\n" + y);
            else
                TextCode = "";

            Code.Document.Text = TextCode;
        }

        private void RearrangeCommnads()
        {
            var ident = 0;
            foreach (var icmd in Commands)
            {
                icmd.PropertyChanged -= Icmd_PropertyChanged;
                icmd.PropertyChanged += Icmd_PropertyChanged;
                IsInputExist(icmd);
                if ((icmd?.Action.IsBlock).GetValueOrDefault(false))
                {
                    icmd.Ident = new Thickness(ident, 0, 0, 0);
                    ident += 8;
                    continue;
                }
                if (icmd?.Action.Function == NativeFunctions.CONDITIONEND)
                    ident -= 8;

                if (ident < 0) ident = 0;

                icmd.Ident = new Thickness(ident, 0, 0, 0);
                //GenerateCode();

            }
        }

        private void Icmd_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "InputKey")
            {
                var s = (sender as vMixControlButtonCommand);
                IsInputExist(s);
            }
        }

        private void IsInputExist(vMixControlButtonCommand s)
        {
            var key = Utils.FindInputKeyByVariable(s.InputKey, Dispatcher);
            var l = (ViewModel.ViewModelLocator)TryFindResource("Locator");
            var check = l.WidgetSettings.Model?.Inputs.Where(x =>
            {
                int number;
                return x.Key == key || x.Title == key || (int.TryParse(key, out number) && x.Number == number);
            }).Count() == 0;
            s.NoInputAssigned = check;
        }


        private void ShowMovedItem(int moveTo)
        {
            script.UpdateLayout();
            var item = script.ItemContainerGenerator.ContainerFromIndex(moveTo) as ListViewItem;
            if (item != null)
            {

                var border = item.Template.FindName("border", item);
                if (border != null)
                    ((Storyboard)FindResource("Blink")).Begin((FrameworkElement)border);
                item.BringIntoView();
            }
        }

        /// <summary>
        /// The <see cref="TextCode" /> property's name.
        /// </summary>
        public const string TextCodePropertyName = "TextCode";

        private string _textCode = "";

        /// <summary>
        /// Sets and gets the TextCode property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string TextCode
        {
            get
            {
                return _textCode;
            }

            set
            {
                if (_textCode == value)
                {
                    return;
                }

                _textCode = value;

                RaisePropertyChanged(TextCodePropertyName);
            }
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

        /// <summary>
        /// Идентифицирует свойство зависимостей <see cref="Log"/>.
        /// </summary>
        public static readonly DependencyProperty LogProperty =
            DependencyProperty.Register(
                nameof(Log),
                typeof(string),
                typeof(ScriptControl), // <-- Замените YourControl на имя вашего класса
                new PropertyMetadata("")); // Значение по умолчанию - пустая строка

        /// <summary>
        /// Получает или задает свойство Log.
        /// Это свойство зависимостей.
        /// </summary>
        public string Log
        {
            get { return (string)GetValue(LogProperty); }
            set { SetValue(LogProperty, value); }
        }

        // <summary>
        /// Идентифицирует свойство зависимостей <see cref="Commands"/>.
        /// </summary>
        public static readonly DependencyProperty CommandsProperty =
            DependencyProperty.Register(
                nameof(Commands),
                typeof(ObservableCollection<vMixControlButtonCommand>),
                typeof(ScriptControl),
                new PropertyMetadata(null, CommandsChangedCallback)); // По умолчанию null, чтобы избежать совместного использования одной коллекции между экземплярами

        private static void CommandsChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (ScriptControl)d;
            var ov = (ObservableCollection<vMixControlButtonCommand>)e.OldValue;
            var nv = (ObservableCollection<vMixControlButtonCommand>)e.NewValue;
            ctrl.GenerateCode();
            if (ov != null)
                ov.CollectionChanged -= ((ScriptControl)d).OnCommandsChanged;
            if (nv != null)
                nv.CollectionChanged += ((ScriptControl)d).OnCommandsChanged;
        }

        /// <summary>
        /// Получает или задает коллекцию команд для кнопки.
        /// Это свойство зависимостей.
        /// </summary>
        public ObservableCollection<vMixControlButtonCommand> Commands
        {
            get { return (ObservableCollection<vMixControlButtonCommand>)GetValue(CommandsProperty); }
            set { SetValue(CommandsProperty, value); }
        }


        private RelayCommand<vMixControlButtonCommand> _removeCommandCommand;

        /// <summary>
        /// Gets the RemoveCommandCommand.
        /// </summary>
        public RelayCommand<vMixControlButtonCommand> RemoveCommandCommand
        {
            get
            {
                return _removeCommandCommand
                    ?? (_removeCommandCommand = new RelayCommand<vMixControlButtonCommand>(
                    p =>
                    {
                        Commands.Remove(p);
                        RearrangeCommnads();
                        //CollectionViewSource.GetDefaultView(script.ItemsSource)?.Refresh();
                    }));
            }
        }

        private RelayCommand _addCommandCommand;

        /// <summary>
        /// Gets the AddCommandCommand.
        /// </summary>
        public RelayCommand AddCommandCommand
        {
            get
            {
                return _addCommandCommand
                    ?? (_addCommandCommand = new RelayCommand(
                    () =>
                    {
                        var cmd = new vMixControlButtonCommand() { Action = new Classes.vMixFunctionReference() };
                        for (int i = 0; i < 10; i++)
                            cmd.AdditionalParameters.Add(new One<string>() { A = "" });
                        Commands.Add(cmd);
                        var index = Math.Max(Commands.Count - 2, 0);
                        RearrangeCommnads();

                        bottomMarker.BringIntoView();
                        
                    }));
            }
        }

        private RelayCommand _exportScriptCommand;

        /// <summary>
        /// Gets the ExportScriptCommand.
        /// </summary>
        public RelayCommand ExportScriptCommand
        {
            get
            {
                return _exportScriptCommand
                    ?? (_exportScriptCommand = new RelayCommand(
                    () =>
                    {
                        Ookii.Dialogs.Wpf.VistaSaveFileDialog opendlg = new Ookii.Dialogs.Wpf.VistaSaveFileDialog
                        {
                            Filter = "UTC Script File|*.usf",
                            DefaultExt = "usf"
                        };
                        var result = opendlg.ShowDialog(App.Current.Windows.OfType<vMixWidgetSettingsView>().FirstOrDefault());
                        if (result.HasValue && result.Value)
                        {
                            XmlSerializer s = new XmlSerializer(typeof(ObservableCollection<vMixControlButtonCommand>));
                            using (var fs = new FileStream(opendlg.FileName, FileMode.Create))
                                s.Serialize(fs, Commands);
                        }

                    }));
            }
        }

        private RelayCommand _importScriptCommand;

        /// <summary>
        /// Gets the ImportScriptCommand.
        /// </summary>
        public RelayCommand ImportScriptCommand
        {
            get
            {
                return _importScriptCommand
                    ?? (_importScriptCommand = new RelayCommand(
                    () =>
                    {
                        Ookii.Dialogs.Wpf.VistaOpenFileDialog opendlg = new Ookii.Dialogs.Wpf.VistaOpenFileDialog
                        {
                            Filter = "UTC Script File|*.usf",
                            DefaultExt = "usf"
                        };
                        var result = opendlg.ShowDialog(App.Current.Windows.OfType<vMixWidgetSettingsView>().FirstOrDefault());
                        if (result.HasValue && result.Value)
                        {
                            try
                            {
                                XmlSerializer s = new XmlSerializer(typeof(ObservableCollection<vMixControlButtonCommand>));
                                using (var fs = new FileStream(opendlg.FileName, FileMode.Open))
                                {
                                    var temp = (ObservableCollection<vMixControlButtonCommand>)s.Deserialize(fs);
                                    Commands.Clear();
                                    foreach (var item in temp)
                                    {
                                        Commands.Add(item);
                                    }
                                }
                                RearrangeCommnads();
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }));
            }
        }

        private RelayCommand _clearScriptCommand;

        /// <summary>
        /// Gets the ClearScriptCommand.
        /// </summary>
        public RelayCommand ClearScriptCommand
        {
            get
            {
                return _clearScriptCommand
                    ?? (_clearScriptCommand = new RelayCommand(
                    () =>
                    {
                        Commands.Clear();
                    }));
            }
        }

        private RelayCommand<vMixControlButtonCommand> _moveCommandUpCommand;

        /// <summary>
        /// Gets the MoveCommandUpCommand.
        /// </summary>
        public RelayCommand<vMixControlButtonCommand> MoveCommandUpCommand
        {
            get
            {
                return _moveCommandUpCommand
                    ?? (_moveCommandUpCommand = new RelayCommand<vMixControlButtonCommand>(
                    p =>
                    {
                        var idx = Commands.IndexOf(p);
                        var moveTo = idx - 1 >= 0 ? idx - 1 : idx;
                        Commands.Move(idx, moveTo);
                        CollectionViewSource.GetDefaultView(script.ItemsSource)?.Refresh();
                        RearrangeCommnads();

                        ShowMovedItem(moveTo);

                    }));
            }
        }

        private RelayCommand<vMixControlButtonCommand> _moveCommandDownCommand;

        public event PropertyChangedEventHandler PropertyChanged;
        internal void RaisePropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        /// <summary>
        /// Gets the MoveCommandDownCommand.
        /// </summary>
        public RelayCommand<vMixControlButtonCommand> MoveCommandDownCommand
        {
            get
            {
                return _moveCommandDownCommand
                    ?? (_moveCommandDownCommand = new RelayCommand<vMixControlButtonCommand>(
                    p =>
                    {
                        var idx = Commands.IndexOf(p);
                        var moveTo = idx + 1 < Commands.Count ? idx + 1 : idx;
                        Commands.Move(idx, moveTo);
                        RearrangeCommnads();

                        ShowMovedItem(moveTo);
                    }));
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as TabControl).SelectedIndex == 1)
            {
                //GenerateCode();
                Code.Select(0, 0);
            }
        }

        private void BindableAvalonEditor_LostFocus(object sender, RoutedEventArgs e)
        {
            TextCode = Code.Document.Text;
            if (!string.IsNullOrWhiteSpace(TextCode))
            {
                var code = TextCode.Split('\r', '\n').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                Commands.Clear();
                foreach (var line in code)
                    Commands.Add(vMixControlButtonCommand.FromString(line));
                //_prevIndex = 0;
            }
        }

        private void BindableAvalonEditor_GotFocus(object sender, RoutedEventArgs e)
        {
            //_prevIndex++;
        }

        private void Func_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Keyboard.ClearFocus();
                var parent = ((ComboBox)sender).Parent;

                while (parent is FrameworkElement && ((FrameworkElement)parent).Parent != null && !(parent is Grid))
                    parent = ((FrameworkElement)parent).Parent;
                while (parent is FrameworkElement && VisualTreeHelper.GetParent(parent) != null && !(parent is Grid))
                    parent = VisualTreeHelper.GetParent(parent);

                FocusManager.SetFocusedElement(parent, (IInputElement)parent);
                ((FrameworkElement)parent).MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                e.Handled = true;
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var value = (int?)(sender as MenuItem).DataContext.GetType().GetProperty("Index")?.GetValue((sender as MenuItem).DataContext);
            if (value.HasValue)
                ((sender as MenuItem).Tag as vMixControlButtonCommand).Parameter = value.Value.ToString();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).ContextMenu.Tag = (sender as Button).Tag;
            (sender as Button).ContextMenu.DataContext = (sender as Button).DataContext;
            if ((sender as Button).ContextMenu.HasItems)
                (sender as Button).ContextMenu.IsOpen = true;
        }

        private void Button_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (!(sender as Button).ContextMenu.HasItems)
            {
                (sender as Button).ContextMenu.IsOpen = false;
                e.Handled = true;
            }
        }
    }
}
