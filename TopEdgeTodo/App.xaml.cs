using System;
using System.Windows;
using TopEdgeTodo.Services;

namespace TopEdgeTodo
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DpiAwarenessHelper.EnsureDpiAwareness();
            ToastHelper.EnsureShortcut();
        }
    }
}
