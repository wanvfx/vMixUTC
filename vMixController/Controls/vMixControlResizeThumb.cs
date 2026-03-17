using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using vMixController.Messages;
using vMixControllerSkin;

namespace vMixController.Controls
{
    public class vMixControlResizeThumb : DraggableThumb
    {
        public vMixControlResizeThumb()
        {
            DragDelta += new DragDeltaEventHandler(this.ResizeThumb_DragDelta);
            DragStarted += ResizeThumb_DragStarted;
            DragCompleted += ResizeThumb_DragCompleted;
        }

        private void ResizeThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            if (DataContext is vMixController.Widgets.vMixControl item && !item.Locked)
                GalaSoft.MvvmLight.Messaging.Messenger.Default.Send(new WidgetEditMessage { Widget = item, Action = WidgetEditAction.Resize, IsStarted = true });
        }

        private void ResizeThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (DataContext is vMixController.Widgets.vMixControl item && !item.Locked)
                GalaSoft.MvvmLight.Messaging.Messenger.Default.Send(new WidgetEditMessage { Widget = item, Action = WidgetEditAction.Resize, IsStarted = false });
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {

            if (this.DataContext is vMixController.Widgets.vMixControl item && !item.Locked)
            {
                double deltaHorizontal = 0;
                double deltaVertical = 0;
                var prevLeft = item.Left;
                var prevTop = item.Top;

                switch (HorizontalAlignment)
                {
                    case System.Windows.HorizontalAlignment.Right:

                        deltaHorizontal = Math.Min(-e.HorizontalChange, item.Width - 64);
                        item.Width -= deltaHorizontal;
                        break;
                    case System.Windows.HorizontalAlignment.Left:
                        deltaHorizontal = Math.Min(e.HorizontalChange, item.Width - 64);
                        // Запоминаем Left до выравнивания
                        prevLeft = item.Left;
                        item.Left += deltaHorizontal;
                        item.Left = Math.Floor(item.Left / 8.0) * 8.0;

                        break;
                    default:
                        break;
                }

                switch (VerticalAlignment)
                {
                    case System.Windows.VerticalAlignment.Bottom:
                        deltaVertical = Math.Min(-e.VerticalChange, item.Height - 8);
                        item.Height -= deltaVertical;
                        item.AlignByGrid();
                        break;
                    case System.Windows.VerticalAlignment.Top:
                        deltaVertical = Math.Min(e.VerticalChange, item.Height - 8);
                        prevTop = item.Top;
                        item.Top += deltaVertical;
                        item.Top = Math.Floor(item.Top / 8.0) * 8.0;
                        break;
                    default:
                        break;
                }

                item.Width -= item.Left - prevLeft;
                item.Width = Math.Floor(item.Width / 8.0) * 8.0;
                if (item.Width < 64)
                {
                    item.Width = 64;
                    item.Left = prevLeft;
                }

                item.Height -= item.Top - prevTop;
                item.Height = Math.Floor(item.Height / 8.0) * 8.0;
                if (item.Height < 8)
                {
                    item.Height = 8;
                    item.Top = prevTop;
                }

                item.AlignByGrid();

            }

            e.Handled = true;
        }
    }
}
