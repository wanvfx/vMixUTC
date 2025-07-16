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
    public class IntegerToButtonImageTypeConverter : MarkupExtension, IValueConverter
    {
        private static IValueConverter _instance;

        /// <summary>
        /// Static instance of this converter.
        /// </summary>
        public static IValueConverter Instance => _instance ?? (_instance = new IntegerToButtonImageTypeConverter());
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int)
                return (int)value == 1 ? "Default" : "Default+Pressed";
            return "Default";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Instance;
            //throw new NotImplementedException();
        }
    }
}
