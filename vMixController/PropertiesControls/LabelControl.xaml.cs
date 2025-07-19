using System.Windows;
using System.Windows.Controls;

namespace vMixController.PropertiesControls
{
    /// <summary>
    /// Логика взаимодействия для IntControl.xaml
    /// </summary>
    public partial class LabelControl : UserControl
    {
        public LabelControl()
        {
            InitializeComponent();
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(LabelControl), new PropertyMetadata(""));

        public string Help
        {
            get { return (string)GetValue(HelpProperty); }
            set { SetValue(HelpProperty, value); }
        }

        public static readonly DependencyProperty HelpProperty =
            DependencyProperty.Register(nameof(Help), typeof(string), typeof(LabelControl), new PropertyMetadata(""));
    }
}