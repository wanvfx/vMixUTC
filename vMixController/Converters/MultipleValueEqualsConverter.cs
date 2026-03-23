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
    public class MultipleValueEqualsConverter : MarkupExtension, IMultiValueConverter
    {
        private static IMultiValueConverter _instance;

        /// <summary>
        /// Static instance of this converter.
        /// </summary>
        public static IMultiValueConverter Instance => _instance ?? (_instance = new MultipleValueEqualsConverter());

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool equals = true;
            for (int i = 0; i < values.Length - 1; i++)
                equals = equals && (values[i]?.ToString() == values[i+1]?.ToString());
            return equals;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Instance;
        }
    }
}
