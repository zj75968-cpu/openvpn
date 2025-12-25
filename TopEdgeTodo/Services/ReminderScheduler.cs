using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using TopEdgeTodo.Models;

namespace TopEdgeTodo.Services
{
    public class ReminderScheduler : IDisposable
    {
        private readonly DispatcherTimer _timer;
        private readonly SettingsService _settings;
        private readonly HashSet<Guid> _notifiedToday = new();
        private Func<IEnumerable<TodoItem>> _itemsProvider = Enumerable.Empty<TodoItem>;

        public ReminderScheduler(SettingsService settings)
        {
            _settings = settings;
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _timer.Tick += (_, _) => Scan();
        }

        public void Start(Func<IEnumerable<TodoItem>> provider)
        {
            _itemsProvider = provider;
            _timer.Start();
        }

        public void Dispose()
        {
            _timer.Stop();
        }

        private void Scan()
        {
            var now = DateTime.Now;
            foreach (var item in _itemsProvider())
            {
                if (item.IsCompleted || !item.DueAt.HasValue)
                {
                    continue;
                }
                if (_notifiedToday.Contains(item.Id))
                {
                    continue;
                }
                var due = item.DueAt.Value;
                var notifyAt = due.AddMinutes(-_settings.ReminderLeadMinutes);
                if (now >= notifyAt && now <= due.AddMinutes(1))
                {
                    var content = $"Project: {item.Project} | Due: {item.DueAt:MM/dd HH:mm}";
                    ToastHelper.ShowToast(item.Title, content, item.Id.ToString());
                    _notifiedToday.Add(item.Id);
                }
            }

            if (_notifiedToday.Count > 200)
            {
                _notifiedToday.Clear();
            }
        }
    }
}
