using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Data;
using TopEdgeTodo.Helpers;
using TopEdgeTodo.Models;
using TopEdgeTodo.Repositories;
using TopEdgeTodo.Services;

namespace TopEdgeTodo.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly TodoRepository _repository;
        private readonly SettingsService _settings;
        private readonly ReminderScheduler _reminderScheduler;
        private readonly string[] _projects = new[] { "All", "Work", "Personal" };

        private string _selectedProject = "All";
        private bool _todayOnly;
        private string _searchText = string.Empty;
        private string _newTitle = string.Empty;
        private string _newProject = "Work";
        private DateTime? _newDueAt;
        private Priority _newPriority = Priority.Medium;
        private bool _showCompleted = true;

        private int _todayCompletedCount;
        private int _incompleteCount;
        private int _workPendingCount;
        private int _personalPendingCount;

        public ObservableCollection<TodoItem> Items { get; } = new();
        public ICollectionView View { get; }
        public SettingsService Settings => _settings;

        public IEnumerable<string> Projects => _projects;

        public RelayCommand AddCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand ToggleCompletedCommand { get; }
        public RelayCommand ExportCommand { get; }
        public RelayCommand ToggleCompletedVisibilityCommand { get; }

        public string SelectedProject
        {
            get => _selectedProject;
            set
            {
                SetProperty(ref _selectedProject, value);
                View.Refresh();
                UpdateStats();
            }
        }

        public bool TodayOnly
        {
            get => _todayOnly;
            set
            {
                SetProperty(ref _todayOnly, value);
                View.Refresh();
                UpdateStats();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                View.Refresh();
            }
        }

        public string NewTitle
        {
            get => _newTitle;
            set
            {
                SetProperty(ref _newTitle, value);
                AddCommand.RaiseCanExecuteChanged();
            }
        }

        public string NewProject
        {
            get => _newProject;
            set => SetProperty(ref _newProject, value);
        }

        public DateTime? NewDueAt
        {
            get => _newDueAt;
            set => SetProperty(ref _newDueAt, value);
        }

        public Priority NewPriority
        {
            get => _newPriority;
            set => SetProperty(ref _newPriority, value);
        }

        public bool ShowCompleted
        {
            get => _showCompleted;
            set
            {
                SetProperty(ref _showCompleted, value);
                View.Refresh();
            }
        }

        public int TodayCompletedCount
        {
            get => _todayCompletedCount;
            private set => SetProperty(ref _todayCompletedCount, value);
        }

        public int IncompleteCount
        {
            get => _incompleteCount;
            private set => SetProperty(ref _incompleteCount, value);
        }

        public int WorkPendingCount
        {
            get => _workPendingCount;
            private set => SetProperty(ref _workPendingCount, value);
        }

        public int PersonalPendingCount
        {
            get => _personalPendingCount;
            private set => SetProperty(ref _personalPendingCount, value);
        }

        public MainViewModel()
        {
            _settings = SettingsService.Load();
            _repository = new TodoRepository(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TopEdgeTodo", "todos.db"));
            foreach (var item in _repository.GetAll())
            {
                Items.Add(item);
            }

            View = CollectionViewSource.GetDefaultView(Items);
            View.Filter = FilterItem;
            if (View is ListCollectionView listView)
            {
                listView.CustomSort = new TodoComparer();
            }

            AddCommand = new RelayCommand(_ => AddItem(), _ => !string.IsNullOrWhiteSpace(NewTitle));
            DeleteCommand = new RelayCommand(item => DeleteItem(item as TodoItem));
            ToggleCompletedCommand = new RelayCommand(item => ToggleCompleted(item as TodoItem));
            ExportCommand = new RelayCommand(_ => Export());
            ToggleCompletedVisibilityCommand = new RelayCommand(_ => { ShowCompleted = !ShowCompleted; });

            _reminderScheduler = new ReminderScheduler(_settings);
            _reminderScheduler.Start(() => Items.ToList());

            UpdateStats();
        }

        private bool FilterItem(object obj)
        {
            if (obj is not TodoItem item)
            {
                return false;
            }

            if (!ShowCompleted && item.IsCompleted)
            {
                return false;
            }

            if (SelectedProject != "All" && !string.Equals(item.Project, SelectedProject, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(SearchText) && !item.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (TodayOnly)
            {
                var start = DateTime.Today;
                var end = start.AddDays(1).AddTicks(-1);
                var isOverdue = item.DueAt.HasValue && item.DueAt.Value < start && !item.IsCompleted;
                var isTodayDue = item.DueAt.HasValue && item.DueAt.Value >= start && item.DueAt.Value <= end;
                if (!isOverdue && !isTodayDue)
                {
                    return false;
                }
            }

            return true;
        }

        private void AddItem()
        {
            var item = new TodoItem
            {
                Title = NewTitle.Trim(),
                Project = NewProject,
                CreatedAt = DateTime.Now,
                DueAt = NewDueAt,
                Priority = NewPriority
            };
            Items.Add(item);
            _repository.Upsert(item);
            Refresh();

            NewTitle = string.Empty;
            NewDueAt = null;
            NewPriority = Priority.Medium;
        }

        private void DeleteItem(TodoItem? item)
        {
            if (item == null)
            {
                return;
            }
            Items.Remove(item);
            _repository.Delete(item.Id);
            Refresh();
        }

        private void ToggleCompleted(TodoItem? item)
        {
            if (item == null)
            {
                return;
            }
            item.IsCompleted = !item.IsCompleted;
            item.CompletedAt = item.IsCompleted ? DateTime.Now : null;
            _repository.Upsert(item);
            Refresh();
        }

        private void Export()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "todos_export.csv");
            var filter = View.Filter;
            var scope = filter == null ? Items.ToList() : Items.Where(i => filter(i)).ToList();
            CsvExporter.Export(scope, path);
        }

        private void Refresh()
        {
            View.Refresh();
            UpdateStats();
            if (View is ListCollectionView listView)
            {
                listView.CustomSort = new TodoComparer();
            }
        }

        private void UpdateStats()
        {
            TodayCompletedCount = Items.Count(i => i.CompletedAt?.Date == DateTime.Today);
            IncompleteCount = Items.Count(i => !i.IsCompleted);
            WorkPendingCount = Items.Count(i => !i.IsCompleted && i.Project.Equals("Work", StringComparison.OrdinalIgnoreCase));
            PersonalPendingCount = Items.Count(i => !i.IsCompleted && i.Project.Equals("Personal", StringComparison.OrdinalIgnoreCase));
        }

        private class TodoComparer : IComparer<object>
        {
            public int Compare(object? x, object? y)
            {
                if (x is not TodoItem a || y is not TodoItem b)
                {
                    return 0;
                }

                // Incomplete first
                int incompleteCompare = a.IsCompleted.CompareTo(b.IsCompleted);
                if (incompleteCompare != 0)
                {
                    return incompleteCompare;
                }

                // Due date ascending, nulls last
                if (a.DueAt.HasValue && b.DueAt.HasValue)
                {
                    int dueCompare = DateTime.Compare(a.DueAt.Value, b.DueAt.Value);
                    if (dueCompare != 0) return dueCompare;
                }
                else if (a.DueAt.HasValue != b.DueAt.HasValue)
                {
                    return a.DueAt.HasValue ? -1 : 1;
                }

                // Priority
                int priorityCompare = a.Priority.CompareTo(b.Priority);
                if (priorityCompare != 0)
                {
                    return priorityCompare;
                }

                // Created time fallback
                return DateTime.Compare(a.CreatedAt, b.CreatedAt);
            }
        }
    }
}
