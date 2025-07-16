using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;
using vMixController.Extensions;

namespace vMixController.Converters
{

    public class ObservableCollectionCopyConverter : MarkupExtension, IValueConverter
    {
        private static IValueConverter _instance;

        /// <summary>
        /// Static instance of this converter.
        /// </summary>
        public static IValueConverter Instance => _instance ?? (_instance = new ObservableCollectionCopyConverter());
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ObjectCopier.Copy(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ObjectCopier.Copy(value);
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Instance;
        }
    }
}
