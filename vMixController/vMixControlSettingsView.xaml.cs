using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using vMixController.Classes;
using vMixController.Extensions;

namespace vMixController
{
    /// <summary>
    /// Description for vMixWidgetSettingsView.
    /// </summary>
    public partial class vMixWidgetSettingsView : Window
    {
        private const int GWL_STYLE = -16;
        private const int WS_MINIMIZEBOX = 0x20000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        static vMixWidgetSettingsView _instance;
        public static vMixWidgetSettingsView Instance { get {
                return _instance ?? (_instance = new vMixWidgetSettingsView());

            } }

        /// <summary>
        /// Initializes a new instance of the vMixWidgetSettingsView class.
        /// </summary>
        public vMixWidgetSettingsView()
        {

            InitializeComponent();
            GalaSoft.MvvmLight.Messaging.Messenger.Default.Register<bool>(this, x =>
            {
                DialogResult = x;
            });
            Closed += (o, e) => Messenger.Default.Unregister(this);

            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            /*e.Cancel = true;
            this.Hide();*/
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            long value = GetWindowLong(hwnd, GWL_STYLE);
            SetWindowLong(hwnd, GWL_STYLE, (int)(value & ~WS_MINIMIZEBOX));
            Topmost = Application.Current?.MainWindow?.Topmost ?? false;
            Activate();
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {

        }

        private void SaveTemplateClick(object sender, RoutedEventArgs e)
        {
            if (VisualTreeHelper.GetChildrenCount(this) > 0)
            {
                var templateRootElement = VisualTreeHelper.GetChild(this.WidgetProperties, 0);

                // Теперь вызываем наш хелпер, но в качестве стартовой точки
                // передаем не все окно (this), а только корневой элемент шаблона.
                templateRootElement?.UpdateAllExplicitSources();
                CommonProperties.UpdateAllExplicitSources();
            }
            
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            this.SetIsCancelledOnAllChildren();
        }

        public void SaveConnectedWidgetProperties()
        {
            if (VisualTreeHelper.GetChildrenCount(this) > 0)
            {
                var templateRootElement = VisualTreeHelper.GetChild(this.WidgetProperties, 0);

                // Теперь вызываем наш хелпер, но в качестве стартовой точки
                // передаем не все окно (this), а только корневой элемент шаблона.
                templateRootElement?.UpdateAllExplicitSources();

                CommonProperties.UpdateAllExplicitSources();

            }
            

        }
    }
}