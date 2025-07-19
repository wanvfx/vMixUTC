using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using vMixController.Classes;

namespace vMixController.PropertiesControls
{
    /// <summary>
    /// Логика взаимодействия для DataSourceControl.xaml
    /// </summary>
    public partial class DataSourceControl : UserControl
    {

        public ObservableCollection<string> Sources
        {
            get { return (ObservableCollection<string>)GetValue(SourcesProperty); }
            set { SetValue(SourcesProperty, value); }
        }

        public static readonly DependencyProperty SourcesProperty =
            DependencyProperty.Register(nameof(Sources), typeof(ObservableCollection<string>), typeof(DataSourceControl), new PropertyMetadata(null));

        public ObservableCollection<string> Properties
        {
            get { return (ObservableCollection<string>)GetValue(PropertiesProperty); }
            set { SetValue(PropertiesProperty, value); }
        }

        public static readonly DependencyProperty PropertiesProperty =
            DependencyProperty.Register(nameof(Properties), typeof(ObservableCollection<string>), typeof(DataSourceControl), new PropertyMetadata(null));

        public string DataSource
        {
            get { return (string)GetValue(DataSourceProperty); }
            set { SetValue(DataSourceProperty, value); }
        }

        
        public static readonly DependencyProperty DataSourceProperty =
            DependencyProperty.Register(nameof(DataSource), typeof(string), typeof(DataSourceControl), new PropertyMetadata(""));

        public string DataProperty
        {
            get { return (string)GetValue(DataPropertyProperty); }
            set { SetValue(DataPropertyProperty, value); }
        }

        public static readonly DependencyProperty DataPropertyProperty =
            DependencyProperty.Register(nameof(DataProperty), typeof(string), typeof(DataSourceControl), new PropertyMetadata(""));


        public bool Active
        {
            get { return (bool)GetValue(ActiveProperty); }
            set { SetValue(ActiveProperty, value); }
        }

        public static readonly DependencyProperty ActiveProperty =
            DependencyProperty.Register(nameof(Active), typeof(bool), typeof(DataSourceControl), new PropertyMetadata(false));


        public void Update()
        {
            Sources = new ObservableCollection<string>(Singleton<SharedData>.Instance.GetDataSources());
        }

        public DataSourceControl()
        {
            InitializeComponent();
            Sources = new ObservableCollection<string>(Singleton<SharedData>.Instance.GetDataSources());
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties = new ObservableCollection<string>(Singleton<SharedData>.Instance.GetDataSourceProps(DataSource));
        }
    }
}