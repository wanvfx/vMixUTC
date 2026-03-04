using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace vMixController.Converters
{
    public class Int32ToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            int v;
            int p;

            try
            {
                v = System.Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return Visibility.Collapsed;
            }

            if (parameter is int pi)
            {
                p = pi;
            }
            else if (!int.TryParse(parameter.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out p))
            {
                return Visibility.Collapsed;
            }

            return v == p ? Visibility.Visible : Visibility.Collapsed;
            //throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
