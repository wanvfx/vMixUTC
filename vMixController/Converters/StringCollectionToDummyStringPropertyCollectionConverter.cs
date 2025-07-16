using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;
using vMixController.Classes;

namespace vMixController.Converters
{
    [ValueConversion(typeof(ObservableCollection<string>), typeof(ObservableCollection<DummyStringProperty>))]
    public class StringCollectionToDummyStringPropertyCollectionConverter : MarkupExtension, IValueConverter
    {
        private static IValueConverter _instance;

        /// <summary>
        /// Static instance of this converter.
        /// </summary>
        public static IValueConverter Instance => _instance ?? (_instance = new StringCollectionToDummyStringPropertyCollectionConverter());

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = new ObservableCollection<DummyStringProperty>();
            if (value != null)
                foreach (var str in (ObservableCollection<string>)value)
                    result.Add(new DummyStringProperty() { Value = str });
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = new ObservableCollection<string>();
            if (value != null)
                foreach (var str in (ObservableCollection<DummyStringProperty>)value)
                    result.Add(str.Value);
            return result;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Instance;
        }
    }
}
