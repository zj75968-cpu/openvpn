using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using TopEdgeTodo.Models;

namespace TopEdgeTodo.Repositories
{
    public class TodoRepository
    {
        private readonly string _connectionString;

        public TodoRepository(string databasePath)
        {
            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();
            Initialize();
        }

        private void Initialize()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            const string sql = @"CREATE TABLE IF NOT EXISTS Todos (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                Project TEXT NOT NULL,
                IsCompleted INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                CompletedAt TEXT,
                DueAt TEXT,
                Priority INTEGER NOT NULL
            );";
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        public List<TodoItem> GetAll()
        {
            var items = new List<TodoItem>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Title, Project, IsCompleted, CreatedAt, CompletedAt, DueAt, Priority FROM Todos";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                items.Add(ReadItem(reader));
            }
            return items;
        }

        public void Upsert(TodoItem item)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO Todos (Id, Title, Project, IsCompleted, CreatedAt, CompletedAt, DueAt, Priority)
                                VALUES ($id, $title, $project, $isCompleted, $createdAt, $completedAt, $dueAt, $priority)
                                ON CONFLICT(Id) DO UPDATE SET
                                    Title = excluded.Title,
                                    Project = excluded.Project,
                                    IsCompleted = excluded.IsCompleted,
                                    CreatedAt = excluded.CreatedAt,
                                    CompletedAt = excluded.CompletedAt,
                                    DueAt = excluded.DueAt,
                                    Priority = excluded.Priority";
            cmd.Parameters.AddWithValue("$id", item.Id.ToString());
            cmd.Parameters.AddWithValue("$title", item.Title);
            cmd.Parameters.AddWithValue("$project", item.Project);
            cmd.Parameters.AddWithValue("$isCompleted", item.IsCompleted ? 1 : 0);
            cmd.Parameters.AddWithValue("$createdAt", item.CreatedAt.ToString("o"));
            cmd.Parameters.AddWithValue("$completedAt", item.CompletedAt?.ToString("o"));
            cmd.Parameters.AddWithValue("$dueAt", item.DueAt?.ToString("o"));
            cmd.Parameters.AddWithValue("$priority", (int)item.Priority);
            cmd.ExecuteNonQuery();
        }

        public void Delete(Guid id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Todos WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        private static TodoItem ReadItem(IDataRecord reader)
        {
            return new TodoItem
            {
                Id = Guid.Parse(reader.GetString(0)),
                Title = reader.GetString(1),
                Project = reader.GetString(2),
                IsCompleted = reader.GetInt32(3) == 1,
                CreatedAt = DateTime.Parse(reader.GetString(4)),
                CompletedAt = reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5)),
                DueAt = reader.IsDBNull(6) ? null : DateTime.Parse(reader.GetString(6)),
                Priority = (Priority)reader.GetInt32(7)
            };
        }
    }
}
