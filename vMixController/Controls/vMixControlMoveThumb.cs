using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using vMixController.Classes;
using vMixController.Messages;
using vMixController.Widgets;
using vMixControllerSkin;

namespace vMixController.Controls
{
    public class vMixControlMoveThumb : DraggableThumb, INotifyPropertyChanged
    {
        public bool Locked
        {
            get;
            set;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public vMixControlMoveThumb()
        {
            DragDelta += new DragDeltaEventHandler(this.MoveThumb_DragDelta);
            this.DragStarted += PhotoMoveThumb_DragStarted;
            this.DataContextChanged += VMixControlMoveThumb_DataContextChanged;
            this.DragCompleted += VMixControlMoveThumb_DragCompleted;
        }

        private void VMixControlMoveThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (this.DataContext is vMixControl item && !item.Locked)
            {
                GalaSoft.MvvmLight.Messaging.Messenger.Default.Send(new WidgetMoveStateMessage() { Widget = item, IsStarted = false });
                GalaSoft.MvvmLight.Messaging.Messenger.Default.Send(new WidgetEditMessage { Widget = item, Action = WidgetEditAction.Move, IsStarted = false });
            }
        }

        private void VMixControlMoveThumb_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is vMixControl ctrl)
            {
                Locked = ctrl.Locked;
                ctrl.PropertyChanged += Ctrl_PropertyChanged;
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Locked"));
        }

        private void Ctrl_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                Locked = (sender as Widgets.vMixControl).Locked;
                PropertyChanged(this, e);
            }
        }

        void PhotoMoveThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            vMixController.Widgets.vMixControl item = this.DataContext as vMixController.Widgets.vMixControl;
            GalaSoft.MvvmLight.Messaging.Messenger.Default.Send(new WidgetMoveStateMessage() { Widget = item, IsStarted = true });
            GalaSoft.MvvmLight.Messaging.Messenger.Default.Send(new WidgetEditMessage { Widget = item, Action = WidgetEditAction.Move, IsStarted = true });
            //item.IsSelected = true;
        }

        private void MoveThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {

            if (this.DataContext is vMixControl item && !item.Locked)
            {
                var px = item.Left;
                var py = item.Top;

                item.Left += e.HorizontalChange;
                item.Top += e.VerticalChange;

                item.AlignPositionByGrid();

                GalaSoft.MvvmLight.Messaging.Messenger.Default.Send(new WidgetMoveDeltaMessage() { Widget = item, DeltaX = item.Left - px, DeltaY = item.Top - py });
            }


        }

    }
}
