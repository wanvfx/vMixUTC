using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using vMixController.ViewModel;

namespace vMixController.Widgets
{
    public class vMixControlVariableViewer : vMixControl
    {
        public override bool IsResizeableVertical => true;
        
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(vMixControlVariableViewer), new PropertyMetadata("", InternalPropertyChanged));

        public bool ShowVariableName
        {
            get { return (bool)GetValue(ShowVariableNameProperty); }
            set { SetValue(ShowVariableNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowVariableName.  
        // This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowVariableNameProperty =
            DependencyProperty.Register("ShowVariableName", typeof(bool), typeof(vMixControlVariableViewer), new PropertyMetadata(true, InternalPropertyChanged));

        private static void InternalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// The <see cref="Variable" /> property's name.
        /// </summary>
        public const string VariablePropertyName = "Variable";

        private string _variable = "";//Basic, Basketball, American Football

        /// <summary>
        /// Sets and gets the Style property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Variable
        {
            get
            {
                return _variable;
            }

            set
            {
                if (_variable == value)
                {
                    return;
                }

                _variable = value;

                BindingOperations.ClearBinding(this, TextProperty);
                var globalSettings = ((ViewModelLocator)App.Current.FindResource("Locator"))?.GlobalSettings;
                Binding b = new Binding("B");
                foreach (var variable in globalSettings.Variables)
                    if (variable.A == _variable)
                        b.Source = variable;
                BindingOperations.SetBinding(this, TextProperty, b);

                RaisePropertyChanged(VariablePropertyName);
            }
        }

        public List<string> VariableList { get => ((ViewModelLocator)App.Current.FindResource("Locator"))?.GlobalSettings.Variables.Select(x => x.A).ToList(); }

        public override string Type
        {
            get
            {
                return Extensions.LocalizationManager.Get("VariableViewer");
            }
        }

        public override UserControl[] GetPropertiesControls()
        {
            return base.GetPropertiesControls();
        }

        public override void SetProperties(UserControl[] _controls)
        {

            base.SetProperties(_controls);
        }
    }
}
