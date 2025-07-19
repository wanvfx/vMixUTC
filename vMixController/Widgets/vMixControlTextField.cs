using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using vMixAPI;
using vMixController.Classes;
using vMixController.Converters;
using vMixController.ViewModel;

namespace vMixController.Widgets
{
    [Serializable]
    public class vMixControlTextField : vMixControl
    {

        protected string _defaultValue = null;
        protected static Queue<Triple<DependencyObject, DependencyProperty, DateTime>> DelayedUpdate = new Queue<Triple<DependencyObject, DependencyProperty, DateTime>>();
        private static DispatcherTimer DelayedUpdateTimer = new DispatcherTimer();

        static vMixControlTextField()
        {
            //Delayed binding update
            DelayedUpdateTimer.Interval = TimeSpan.FromSeconds(0.1);
            DelayedUpdateTimer.Tick += DelayedUpdateTimer_Tick;
            DelayedUpdateTimer.Start();
        }

        public override Hotkey[] GetHotkeys()
        {
            return new Classes.Hotkey[] { new Classes.Hotkey() { Name = "Focus" } };
        }

        public override void ExecuteHotkey(int index)
        {
            switch (index)
            {
                case 0:
                    IsFocused = false;
                    IsFocused = true;
                    break;
            }
            base.ExecuteHotkey(index);
        }

        private static void DelayedUpdateTimer_Tick(object sender, EventArgs e)
        {
            while (DelayedUpdate.Count > 0 && DelayedUpdate.Peek().C.AddSeconds(0.1) < DateTime.Now)
            {
                var t = DelayedUpdate.Dequeue();
                try
                {
                    var exp = BindingOperations.GetMultiBindingExpression(t.A, t.B);
                    if (exp != null && exp.Status == BindingStatus.Active && exp.BindingExpressions.Count > 0)
                        exp.UpdateSource();
                }
                catch (Exception) { }
            }
        }

        internal bool _updating = false;
        internal string _text = "";

        public override string Type
        {
            get
            {
                return Extensions.LocalizationManager.Get("Text Field");
            }
        }

        public vMixControlTextField()
        {
            //_paths.CollectionChanged += _paths_CollectionChanged;
        }

        internal override void OnStateSynced()
        {
            UpdateText(_paths);
        }

        /*private void _paths_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (var item in e.NewItems)
                    UpdateText((sender as IList).OfType<Pair<int, string>>().ToList());
        }*/

        internal virtual string MappedTextProperty { get { return "Text"; } }

        internal virtual string MappedImageProperty { get { return "Image"; } }

        protected bool _isLive = true;

        /// <summary>
        /// Sets and gets the IsLive property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsLive
        {
            get
            {
                return _isLive;
            }

            set
            {
                if (_isLive == value)
                {
                    return;
                }

                if (!value && DelayedUpdate.Count > 0)
                    DelayedUpdate.Clear();

                _isLive = value;

                if (value)
                    _text = Text;

                UpdateText(_paths);

                Text = _text;

                RaisePropertyChanged(nameof(IsLive));
            }
        }

        private bool _isTable = false;

        /// <summary>
        /// Sets and gets the IsTable property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsTable
        {
            get
            {
                return _isTable;
            }

            set
            {
                if (_isTable == value)
                {
                    return;
                }

                _isTable = value;
                UpdateText(_paths);
                RaisePropertyChanged(nameof(IsTable));
            }
        }

        private bool _isEditable = true;

        /// <summary>
        /// Sets and gets the IsEditable property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsEditable
        {
            get
            {
                return _isEditable;
            }

            set
            {
                if (_isEditable == value)
                {
                    return;
                }

                _isEditable = value;
                RaisePropertyChanged(nameof(IsEditable));
            }
        }

        private bool _isMappedToGUID = true;

        /// <summary>
        /// Sets and gets the IsMappedToGUID property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsMappedToGUID
        {
            get
            {
                return _isMappedToGUID;
            }

            set
            {
                if (_isMappedToGUID == value)
                {
                    return;
                }

                _isMappedToGUID = value;
                RaisePropertyChanged(nameof(IsMappedToGUID));
            }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(vMixControlTextField), new PropertyMetadata("", InternalPropertyChanged));

        protected static void InternalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!((vMixControlTextField)d).IsLive)
                return;
            if (e.Property.Name == nameof(Text))
            {
                try
                {
                    var exp = BindingOperations.GetMultiBindingExpression(d, TextProperty);
                    if (exp != null && exp.Status == BindingStatus.Active)
                        DelayedUpdate.Enqueue(new Triple<DependencyObject, DependencyProperty, DateTime>() { A = d, B = e.Property, C = DateTime.Now });

                    //((vMixControlTextField)d).OnPropertyChanged(e);
                }
                catch (Exception) { }
            }

            
        }

        internal virtual IMultiValueConverter ConverterSelector()
        {
            if (!IsTable)
                return new FirstValueConverter(def: _defaultValue);
            else
                return new StringsToStringConverter();
        }

        internal virtual void UpdateText(IList<Pair<string, string>> _paths)
        {
            if (!_isLive)
            {
                _text = Text;
                BindingOperations.ClearBinding(this, TextProperty);
                Text = _text;
                return;
            }

            if (!_updating)
            {
                _updating = true;
                _text = Text;
                BindingOperations.ClearBinding(this, TextProperty);
                Text = _text;
                MultiBinding binding = new MultiBinding
                {
                    Converter = ConverterSelector(),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.Default,
                    NotifyOnSourceUpdated = true,
                    NotifyOnTargetUpdated = true
                };
                //binding.Delay = 10;


                InputBase text = null;

                //update text
                if (_paths != null && State != null)
                    foreach (var item in _paths)
                    {
                        var input = (Input)GetValueByPath(State, string.Format("Inputs[{0}]", item.A));
                        if (input != null)
                        {
                            var val = input.Elements.Where(x => (x is InputBase) && (x as InputBase).Name == item.B).FirstOrDefault();

                            if (val != null && val is InputBase && !_isTable)
                            {
                                if (text == null)
                                    text = (val as InputBase);
                                else
                                {
                                    var prop = val.GetType().GetProperty(val is InputImage ? MappedImageProperty : MappedTextProperty);
                                    if (prop != null)
                                    {
                                        var iprop = text.GetType().GetProperty(val is InputImage ? MappedImageProperty : MappedTextProperty);
                                        if (iprop != null)
                                            prop.SetValue(val, iprop.GetValue(text));
                                    }
                                }
                            }

                            Binding b = new Binding(val is InputImage ? MappedImageProperty : MappedTextProperty)
                            {
                                Source = val,
                                Mode = BindingMode.TwoWay,
                                UpdateSourceTrigger = UpdateSourceTrigger.Default,
                                FallbackValue = _defaultValue,
                                TargetNullValue = _defaultValue
                            };

                            binding.Bindings.Add(b);
                            //PresentationTraceSources.SetTraceLevel(b, PresentationTraceLevel.High);
                        }
                    }

                BindingOperations.SetBinding(this, TextProperty, binding);
                //PresentationTraceSources.SetTraceLevel(binding, PresentationTraceLevel.High);

                if (text != null && !_isTable)
                {
                    var iprop = text.GetType().GetProperty(text is InputImage ? MappedImageProperty : MappedTextProperty);
                    if (iprop != null)
                        Text = (string)iprop.GetValue(text);
                }

                _updating = false;
            }
        }

        private ObservableCollection<Pair<string, string>> _paths = new ObservableCollection<Pair<string, string>>();

        /// <summary>
        /// Sets and gets the Paths property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<Pair<string, string>> Paths
        {
            get
            {
                return _paths;
            }

            set
            {
                if (_paths == value)
                {
                    return;
                }

                _paths = value;
                RaisePropertyChanged(nameof(Paths));
            }
        }

        private bool _template = false;

        /// <summary>
        /// Sets and gets the Style property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool Template
        {
            get
            {
                return _template;
            }

            set
            {
                if (_template == value)
                {
                    return;
                }

                _template = value;
                RaisePropertyChanged(nameof(Template));
            }
        }

        public override UserControl[] GetPropertiesControls()
        {
            return base.GetPropertiesControls();
        }

        public override void SetProperties(vMixWidgetSettingsViewModel viewModel)
        {
            base.SetProperties(viewModel);


        }

        [NonSerialized]
        private RelayCommand _selectPathCommand;

        /// <summary>
        /// Gets the SelectPathCommand.
        /// </summary>
        public RelayCommand SelectPathCommand
        {
            get
            {
                return _selectPathCommand
                    ?? (_selectPathCommand = new RelayCommand(
                    () =>
                    {
                        var dialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog
                        {
                            Filter = "All files (*.*)|*.*"
                        };
                        var result = dialog.ShowDialog();
                        if (result.HasValue && result.Value)
                            Text = dialog.FileName;
                    }));
            }
        }

        public override void SetProperties(UserControl[] _controls)
        {
            base.SetProperties(_controls);
            UpdateText(_paths);
        }

        protected override void Dispose(bool managed)
        {
            base.Dispose(managed);
        }

        public override void Update()
        {
            UpdateText(Paths);
            base.Update();
        }
    }
}
