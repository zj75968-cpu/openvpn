using System;

namespace TopEdgeTodo.Models
{
    public enum Priority
    {
        High = 0,
        Medium = 1,
        Low = 2
    }

    public class TodoItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Project { get; set; } = "Work";
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        public DateTime? DueAt { get; set; }
        public Priority Priority { get; set; } = Priority.Medium;
    }
}
