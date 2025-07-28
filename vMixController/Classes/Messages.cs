using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public enum VariableState
    {
        None,
        Delete,
        Add,
        Update,
        Clear
    }

    public class UpdateGlobalVariable
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public VariableState State { get; set; }
    }

    public class FillGlobalVariables
    {
        public object Caller { get; set; }
    }

}