using System;
using System.Globalization;
using System.Linq;
using System.Resources;

namespace UTCNDIMonitorDataProvider.Properties
{
    internal static class Strings
    {
        private const string BaseName = "vMixController.Properties.Strings";

        public static string Get(string key, string fallback = null)
        {
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, "vMixController", StringComparison.OrdinalIgnoreCase));

                if (asm == null)
                    return fallback ?? key;

                var rm = new ResourceManager(BaseName, asm);
                return rm.GetString(key, CultureInfo.CurrentUICulture) ?? (fallback ?? key);
            }
            catch
            {
                return fallback ?? key;
            }
        }

        public static string CpuUnsupported => Get("NDIMonitor.Error.CpuUnsupported", "CPU unsupported.");
        public static string CannotRunNdi => Get("NDIMonitor.Error.CannotRunNdi", "Cannot run NDI.");
    }
}
