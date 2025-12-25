using System;
using System.Runtime.InteropServices;
using System.Timers;

namespace TopEdgeTodo.Services
{
    public class TopEdgeDetector : IDisposable
    {
        private readonly Timer _timer;
        private readonly SettingsService _settings;

        public event EventHandler? TopEdgeReached;

        public TopEdgeDetector(SettingsService settings, int intervalMs = 40)
        {
            _settings = settings;
            _timer = new Timer(intervalMs);
            _timer.Elapsed += OnTick;
            _timer.AutoReset = true;
        }

        public void Start() => _timer.Start();
        public void Stop() => _timer.Stop();

        private void OnTick(object? sender, ElapsedEventArgs e)
        {
            if (_settings.DisableWhenFullscreen && IsForegroundFullscreen())
            {
                return;
            }

            if (GetCursorPos(out var point))
            {
                // Physical pixels are returned; threshold of 1px works across DPIs when per-monitor aware.
                if (point.Y <= 1)
                {
                    TopEdgeReached?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private bool IsForegroundFullscreen()
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
            {
                return false;
            }

            GetWindowRect(hwnd, out var rect);
            var monitor = MonitorFromWindow(hwnd, 2);
            if (monitor == IntPtr.Zero)
            {
                return false;
            }

            MONITORINFO info = new() { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
            if (GetMonitorInfo(monitor, ref info))
            {
                var width = info.rcMonitor.Right - info.rcMonitor.Left;
                var height = info.rcMonitor.Bottom - info.rcMonitor.Top;
                return rect.Left <= info.rcMonitor.Left && rect.Top <= info.rcMonitor.Top &&
                       rect.Right >= info.rcMonitor.Right && rect.Bottom >= info.rcMonitor.Bottom &&
                       width > 0 && height > 0;
            }
            return false;
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }
    }
}
