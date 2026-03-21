using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vMixController
{
    public static class vMixControlUtils
    {
        public const double GridSize = 8.0;

        public static double SnapToGrid(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0;

            return Math.Round(value / GridSize) * GridSize;
        }

        public static void AlignPositionByGrid(this vMixController.Widgets.vMixControl item)
        {
            item.Left = SnapToGrid(item.Left);
            item.Top = SnapToGrid(item.Top);
        }

        public static void AlignSizeByGrid(this vMixController.Widgets.vMixControl item)
        {
            item.Width = SnapToGrid(item.Width);
            item.Height = SnapToGrid(item.Height);

            if (item.Height < GridSize || double.IsNaN(item.Height))
                item.Height = GridSize;
            if (item.Width < 64)
                item.Width = 64;
        }

        public static void AlignByGrid(this vMixController.Widgets.vMixControl item)
        {
            item.AlignPositionByGrid();
            item.AlignSizeByGrid();
        }
    }
}
