using System.Windows;
using System.Windows.Controls;

namespace vMixController.PropertiesControls
{
    /// <summary>
    /// Логика взаимодействия для IntControl.xaml
    /// </summary>
    public partial class StringControl : UserControl
    {
        public StringControl()
        {
            InitializeComponent();
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(StringControl), new PropertyMetadata(""));

        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(string), typeof(StringControl), new PropertyMetadata(""));

        public bool AcceptReturn
        {
            get { return (bool)GetValue(AcceptReturnProperty); }
            set { SetValue(AcceptReturnProperty, value); }
        }

        public static readonly DependencyProperty AcceptReturnProperty =
            DependencyProperty.Register(nameof(AcceptReturn), typeof(bool), typeof(StringControl), new PropertyMetadata(false));

        public TextWrapping TextWrapping
        {
            get { return (TextWrapping)GetValue(TextWrappingProperty); }
            set { SetValue(TextWrappingProperty, value); }
        }

        public static readonly DependencyProperty TextWrappingProperty =
            DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping), typeof(StringControl), new PropertyMetadata(TextWrapping.NoWrap));
    }
}