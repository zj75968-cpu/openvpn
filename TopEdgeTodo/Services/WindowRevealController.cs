using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace TopEdgeTodo.Services
{
    public class WindowRevealController
    {
        private readonly Window _window;
        private readonly SettingsService _settings;
        private readonly DispatcherTimer _hideTimer;
        private bool _isAnimating;
        private bool _isVisible;

        public WindowRevealController(Window window, SettingsService settings)
        {
            _window = window;
            _settings = settings;
            _hideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(settings.AutoHideDelaySeconds)
            };
            _hideTimer.Tick += (_, _) => HideWindow();
        }

        public void Attach()
        {
            _window.Loaded += (_, _) => InitializePosition();
            _window.MouseEnter += (_, _) => _hideTimer.Stop();
            _window.MouseLeave += (_, _) => _hideTimer.Start();
        }

        private void InitializePosition()
        {
            _window.Left = (SystemParameters.PrimaryScreenWidth - _window.Width) / 2;
            _window.Top = -_window.Height;
            _window.ShowInTaskbar = false;
            _window.Topmost = false;
            _window.WindowStyle = WindowStyle.None;
            _window.AllowsTransparency = false;
        }

        public void Reveal()
        {
            if (_isAnimating || _isVisible)
            {
                return;
            }
            _hideTimer.Stop();
            _window.Dispatcher.Invoke(() => AnimateTo(0, show: true));
        }

        public void HideWindow()
        {
            if (_isAnimating || !_isVisible)
            {
                return;
            }
            _window.Dispatcher.Invoke(() => AnimateTo(-_window.Height, show: false));
        }

        private void AnimateTo(double targetTop, bool show)
        {
            _isAnimating = true;
            DoubleAnimation animation = new()
            {
                To = targetTop,
                Duration = TimeSpan.FromMilliseconds(_settings.AnimationDurationMs),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            animation.Completed += (_, _) =>
            {
                _isAnimating = false;
                _isVisible = show;
                if (!_isVisible)
                {
                    _hideTimer.Stop();
                }
            };

            var storyboard = new Storyboard();
            Storyboard.SetTarget(animation, _window);
            Storyboard.SetTargetProperty(animation, new PropertyPath(Window.TopProperty));
            storyboard.Children.Add(animation);

            if (show)
            {
                ShowWindowNoActivate(new System.Windows.Interop.WindowInteropHelper(_window).Handle);
            }
            storyboard.Begin();
        }

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const int SW_SHOWNOACTIVATE = 4;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;

        private void ShowWindowNoActivate(IntPtr handle)
        {
            ShowWindow(handle, SW_SHOWNOACTIVATE);
            SetWindowPos(handle, new IntPtr(-1), 0, 0, 0, 0, SWP_NOACTIVATE | SWP_NOMOVE | SWP_NOSIZE);
        }
    }
}
