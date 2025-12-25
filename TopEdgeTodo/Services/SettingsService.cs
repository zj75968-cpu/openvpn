using System;
using System.IO;
using System.Text.Json;

namespace TopEdgeTodo.Services
{
    public class SettingsService
    {
        private const string FileName = "settings.json";

        public double WindowWidth { get; set; } = 380;
        public double WindowHeight { get; set; } = 520;
        public double AutoHideDelaySeconds { get; set; } = 0.8;
        public double AnimationDurationMs { get; set; } = 200;
        public bool DisableWhenFullscreen { get; set; } = true;
        public int ReminderLeadMinutes { get; set; } = 0;

        public static SettingsService Load()
        {
            try
            {
                var path = GetFilePath();
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<SettingsService>(json) ?? new SettingsService();
                }
            }
            catch
            {
                // fall back to defaults
            }
            return new SettingsService();
        }

        public void Save()
        {
            try
            {
                var path = GetFilePath();
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch
            {
                // best-effort persistence only.
            }
        }

        private static string GetFilePath()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TopEdgeTodo");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, FileName);
        }
    }
}
