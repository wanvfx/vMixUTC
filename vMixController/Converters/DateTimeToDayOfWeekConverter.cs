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
    [ValueConversion(typeof(DateTime), typeof(int), ParameterType = typeof(string))]
    public class DateTimeToDayOfWeekConverter : MarkupExtension, IValueConverter
    {

        private static IValueConverter _instance;

        /// <summary>
        /// Static instance of this converter.
        /// </summary>
        public static IValueConverter Instance => _instance ?? (_instance = new DateTimeToDayOfWeekConverter());

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var date = (Classes.DaysOfWeek)value;
            switch ((string)parameter)
            {
                case "M":
                    return date.HasFlag(Classes.DaysOfWeek.Monday);
                case "T":
                    return date.HasFlag(Classes.DaysOfWeek.Tuesday);
                case "W":
                    return date.HasFlag(Classes.DaysOfWeek.Wednesday);
                case "TH":
                    return date.HasFlag(Classes.DaysOfWeek.Thursday);
                case "F":
                    return date.HasFlag(Classes.DaysOfWeek.Friday);
                case "S":
                    return date.HasFlag(Classes.DaysOfWeek.Saturday);
                case "SU":
                    return date.HasFlag(Classes.DaysOfWeek.Sunday);

            }
            return false;
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
