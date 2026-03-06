using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace vMixController.Converters
{
    public class MatrixToThicknessConverter : MarkupExtension, IValueConverter
    {
        private static IValueConverter _instance;

        /// <summary>
        /// Static instance of this converter.
        /// </summary>
        public static IValueConverter Instance => _instance ?? (_instance = new MatrixToThicknessConverter());
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Matrix m)
            {
                var zero = m.Transform(new System.Windows.Point(0, 0));
                var point = m.Transform(new System.Windows.Point(5, 5));
                return new System.Windows.Thickness(Math.Abs(zero.X - point.X));
            }
            return new System.Windows.Thickness(2);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Instance;
        }
    }
}
