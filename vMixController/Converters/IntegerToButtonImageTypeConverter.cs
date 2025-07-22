using Microsoft.VisualBasic;
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
                return (int)value == 1 ? vMixController.Classes.Constants.BUTTON_IMAGE_TYPE_DEFAULT : vMixController.Classes.Constants.BUTTON_IMAGE_TYPE_DEFAULTPRESSED;
            return vMixController.Classes.Constants.BUTTON_IMAGE_TYPE_DEFAULT;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string)
                return (string)value == vMixController.Classes.Constants.BUTTON_IMAGE_TYPE_DEFAULT ? 1 : 2;
            return 1;
            //throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Instance;
            //throw new NotImplementedException();
        }
    }
}
