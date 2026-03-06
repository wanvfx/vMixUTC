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
            // Выравниваем размеры по сетке без компенсации
            item.Left = Math.Floor(item.Left / 8.0) * 8.0;
            item.Top = Math.Floor(item.Top / 8.0) * 8.0;
            item.Width = Math.Floor(item.Width / 8.0) * 8.0;
            item.Height = Math.Floor(item.Height / 8.0) * 8.0;

            if (item.Height < 8 || double.IsNaN(item.Height))
                item.Height = 8;
            if (item.Width < 64)
                item.Width = 64;
        }
    }
}
