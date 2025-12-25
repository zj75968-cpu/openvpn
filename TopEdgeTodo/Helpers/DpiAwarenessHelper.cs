using System;
using System.Runtime.InteropServices;

namespace TopEdgeTodo.Services
{
    public static class DpiAwarenessHelper
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiFlag);

        private static readonly IntPtr PerMonitorV2 = new(-4);

        public static void EnsureDpiAwareness()
        {
            try
            {
                SetProcessDpiAwarenessContext(PerMonitorV2);
            }
            catch
            {
                // Safe to ignore on older OS.
            }
        }
    }
}
