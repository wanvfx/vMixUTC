
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
using vMixController.PropertiesControls;

namespace vMixController.Converters
{
    public class ByteToPairBoolStringConverter : MarkupExtension, IMultiValueConverter
    {
        private static IMultiValueConverter _instance;

        /// <summary>
        /// Static instance of this converter.
        /// </summary>
        public static IMultiValueConverter Instance => _instance ?? (_instance = new ByteToPairBoolStringConverter());

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var vals = new ObservableCollection<Pair<bool, string>>();
            for (byte i = 0; i < 7; i++)
            {
                var e = new Pair<bool, string>(((byte)values[0]).GetBit(i), ((Hotkey[])values[1]).Skip(1).ToArray()[i].Name);
                vals.Add(e);
                
            }

            return vals;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            byte result = 0;
            var vals = (ObservableCollection<Pair<bool, string>>)value;
            for (byte i = 0; i < 7; i++)
                result = result.SetBit(i, vals[i].A);
            return new object[] { result, null };
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Instance;
        }
    }
}
