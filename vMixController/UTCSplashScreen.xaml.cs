using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
using vMixController.ViewModel;

namespace vMixController
{
    /// <summary>
    /// Логика взаимодействия для UTCSplashScreen.xaml
    /// </summary>
    public partial class UTCSplashScreen : UserControl
    {
        
        public UTCSplashScreen()
        {
            InitializeComponent();
            Build.Text = string.Format(CultureInfo.CreateSpecificCulture("en-US").DateTimeFormat, "Carelessly builded @{0:d} by elgarf, use at your own risk\n{1}", MainViewModel.GetBuildDateTime(Assembly.GetExecutingAssembly()), "HTTPClient w/cache version");
            //this.RenderSize = new System.Windows.Size(400, 225);
        }
    }
}
