using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace vMixController.Converters
{
    public class MatrixInverterWidgetsConverter : MarkupExtension, IMultiValueConverter
    {
        private static IMultiValueConverter _instance;

        /// <summary>
        /// Static instance of this converter.
        /// </summary>
        public static IMultiValueConverter Instance => _instance ?? (_instance = new MatrixInverterWidgetsConverter());
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.Length == 4 &&
                    value[0] is Matrix m &&
                    value[1] is IList<vMixController.Widgets.vMixControl> list &&
                    value[2] is double canvasWidth &&
                    value[3] is double canvasHeight)
            {
                // Real content size
                var minX = Math.Min(list.Select(x => x.Left).Append(0).Min(), 0);
                var minY = Math.Min(list.Select(x => x.Top).Append(0).Min(), 0);
                var maxX = Math.Max(list.Select(x => x.Left + x.Width).Append(canvasWidth).Max(), canvasWidth);
                var maxY = Math.Max(list.Select(x => x.Top + x.Height).Append(canvasHeight).Max(), canvasHeight);

                // Full content width
                var totalWidth = maxX - minX;
                var totalHeight = maxY - minY;

                var visualBrushScale = Math.Min(canvasWidth / totalWidth, canvasHeight / totalHeight);

                var visualBrushOffsetX = (canvasWidth - totalWidth * visualBrushScale) / 2.0;
                var visualBrushOffsetY = (canvasHeight - totalHeight * visualBrushScale) / 2.0;

                m.Invert();

                // Result viewport matrix:
                // 1. Inverted transformation (pan/zoom)
                // 2. Negative coordinates compensation
                // 3. VisualBrush scale
                // 4. VisualBrush center transiion

                var result = new Matrix();

                result.Scale(m.M11, m.M22);

                result.Translate(
                    m.OffsetX * visualBrushScale + (-minX) * visualBrushScale + visualBrushOffsetX,
                    m.OffsetY * visualBrushScale + (-minY) * visualBrushScale + visualBrushOffsetY
                );

                result.M11 *= visualBrushScale;
                result.M22 *= visualBrushScale;

                return result;
            }
            return Transform.Identity;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Instance;
        }
    }
}
