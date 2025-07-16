using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;

namespace vMixController.Converters
{
    public class BoolToTextOrFileConverter : MarkupExtension, IValueConverter
    {
        private static IValueConverter _instance;

        /// <summary>
        /// Static instance of this converter.
        /// </summary>
        public static IValueConverter Instance => _instance ?? (_instance = new BoolToTextOrFileConverter());
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? "File" : "Text";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (string)value == "File";
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Instance;
        }
    }
}
