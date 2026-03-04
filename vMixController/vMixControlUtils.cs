using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vMixController
{
    public static class vMixControlUtils
    {
        public static void AlignByGrid(this vMixController.Widgets.vMixControl item)
        {
            var pleft = item.Left;
            // Use floor-based snapping so negative coordinates are supported (infinite canvas).
            item.Left = Math.Floor(item.Left / 8.0) * 8.0;
            if (pleft > 0)
                item.Width += pleft - item.Left;
            item.Width = ((int)item.Width / 8) * 8;
            item.Height = ((int)item.Height / 8) * 8;
            item.Top = Math.Floor(item.Top / 8.0) * 8.0;
            if (item.Height < 8)
                item.Height = 8;

        }
    }
}
