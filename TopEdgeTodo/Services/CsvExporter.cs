using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TopEdgeTodo.Models;

namespace TopEdgeTodo.Services
{
    public static class CsvExporter
    {
        public static void Export(IEnumerable<TodoItem> items, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("id,title,project,priority,dueAt,isCompleted,createdAt,completedAt");
            foreach (var item in items)
            {
                sb.AppendLine(string.Join(',',
                    Escape(item.Id.ToString()),
                    Escape(item.Title),
                    Escape(item.Project),
                    Escape(item.Priority.ToString()),
                    Escape(item.DueAt?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty),
                    Escape(item.IsCompleted ? "true" : "false"),
                    Escape(item.CreatedAt.ToString("o", CultureInfo.InvariantCulture)),
                    Escape(item.CompletedAt?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty)));
            }
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private static string Escape(string value)
        {
            if (value.Contains('"')) value = value.Replace("\"", "\"\"");
            if (value.Contains(',') || value.Contains('\n'))
            {
                value = $"\"{value}\"";
            }
            return value;
        }
    }
}
