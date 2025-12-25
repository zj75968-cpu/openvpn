using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using CommunityToolkit.WinUI.Notifications;

namespace TopEdgeTodo.Services
{
    public static class ToastHelper
    {
        public const string AppId = "TopEdgeTodo.App";

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);

        private const uint KF_FLAG_DEFAULT = 0x00000000;
        private static readonly Guid FOLDERID_Programs = new("A77F5D77-2E2B-44C3-A6A2-ABA601054A51");

        public static void EnsureShortcut()
        {
            try
            {
                string shortcutPath = GetShortcutPath();
                if (!File.Exists(shortcutPath))
                {
                    CreateShortcut(shortcutPath);
                }
                DesktopNotificationManagerCompat.RegisterAumidAndComServer<AppNotificationActivator>(AppId);
                DesktopNotificationManagerCompat.RegisterActivator<AppNotificationActivator>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Toast initialization failed: {ex.Message}");
            }
        }

        public static void ShowToast(string title, string content, string? arguments = null)
        {
            var toastContent = new ToastContentBuilder()
                .AddText(title)
                .AddText(content);
            if (!string.IsNullOrWhiteSpace(arguments))
            {
                toastContent.AddArgument("action", arguments);
            }
            var toast = new ToastNotification(toastContent.GetXml())
            {
                Tag = "todo"
            };
            DesktopNotificationManagerCompat.CreateToastNotifier().Show(toast);
        }

        private static string GetShortcutPath()
        {
            SHGetKnownFolderPath(FOLDERID_Programs, KF_FLAG_DEFAULT, IntPtr.Zero, out var pPath);
            string programsPath = Marshal.PtrToStringUni(pPath)!;
            Marshal.FreeCoTaskMem(pPath);
            string folder = Path.Combine(programsPath, "TopEdgeTodo");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "TopEdgeTodo.lnk");
        }

        private static void CreateShortcut(string shortcutPath)
        {
            string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
            dynamic? shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!);
            if (shell == null)
            {
                return;
            }

            try
            {
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = exePath;
                shortcut.Arguments = "";
                shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
                shortcut.AppUserModelID = AppId;
                shortcut.Save();
            }
            finally
            {
                Marshal.FinalReleaseComObject(shell);
            }
        }
    }

    // Needed for desktop notifications activation.
    [ComImport]
    [Guid("0FBA1EE7-0E26-4F5A-9917-0291D9FE22DC")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface INotificationActivationCallback
    {
        void Activate([In, MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
                      [In, MarshalAs(UnmanagedType.LPWStr)] string invokedArgs,
                      [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] NotificationUserInput[] data,
                      uint count);
    }

    [ComVisible(true)]
    [Guid("A53DC8A7-7C98-4E7C-B237-91B7B64D30CB")]
    [ClassInterface(ClassInterfaceType.None)]
    public class AppNotificationActivator : NotificationActivator
    {
    }
}
