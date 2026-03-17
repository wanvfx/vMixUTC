using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vMixController.Widgets;

namespace vMixController.Messages
{
    public struct LoadingMessage
    {
        public bool Loading { get; set; }
    }
    public class LIVEToggleMessage
    {
        public int State { get; set; }
    }

    public class SetGlobalVariable
    {
        public int Index { get; set; } = -1;
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public enum WidgetEditAction
    {
        Move,
        Resize
    }

    public class WidgetEditMessage
    {
        public vMixControl Widget { get; set; }
        public WidgetEditAction Action { get; set; }
        public bool IsStarted { get; set; }
    }

}