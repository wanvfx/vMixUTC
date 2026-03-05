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
    public class MatrixInverterConverter : MarkupExtension, IValueConverter
    {
        private static IValueConverter _instance;

        /// <summary>
        /// Static instance of this converter.
        /// </summary>
        public static IValueConverter Instance => _instance ?? (_instance = new MatrixInverterConverter());
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Matrix m)
            {
                m.Invert(); // Инвертируем матрицу: теперь она переводит из экранных координат в координаты контента
                return new MatrixTransform(m);
            }
            return Transform.Identity;
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
