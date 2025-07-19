using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using vMixController.Classes;

namespace vMixController.PropertiesControls
{
    /// <summary>
    /// Логика взаимодействия для IntControl.xaml
    /// </summary>
    public partial class BoolControl : UserControl
    {
        public BoolControl()
        {
            InitializeComponent();
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(BoolControl), new PropertyMetadata(""));

        public bool Value
        {
            get { return (bool)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(bool), typeof(BoolControl), new PropertyMetadata(false));

        public ObservableCollection<Pair<bool, string>> Values
        {
            get { return (ObservableCollection<Pair<bool, string>>)GetValue(ValuesProperty); }
            set { SetValue(ValuesProperty, value); }
        }

        public static readonly DependencyProperty ValuesProperty =
            DependencyProperty.Register(nameof(Values), typeof(ObservableCollection<Pair<bool, string>>), typeof(BoolControl), new PropertyMetadata(null));

        public string Help
        {
            get { return (string)GetValue(HelpProperty); }
            set { SetValue(HelpProperty, value); }
        }

        public static readonly DependencyProperty HelpProperty =
            DependencyProperty.Register(nameof(Help), typeof(string), typeof(BoolControl), new PropertyMetadata(""));

        public bool Grouped
        {
            get { return (bool)GetValue(GroupedProperty); }
            set { SetValue(GroupedProperty, value); }
        }

        public static readonly DependencyProperty GroupedProperty =
            DependencyProperty.Register(nameof(Grouped), typeof(bool), typeof(BoolControl), new PropertyMetadata(false));

    }
}