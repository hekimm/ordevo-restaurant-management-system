using Microsoft.Data.Sqlite;
using System.IO;

namespace Ordevo.Desktop.Wpf.Services;

public sealed class OfflineStore(string configuredPath)
{
    private string DatabasePath { get; } = ResolvePath(configuredPath);

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        await using var db = new SqliteConnection($"Data Source={DatabasePath}");
        await db.OpenAsync(ct);
        await using var command = db.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS cache_entries (
              cache_key TEXT PRIMARY KEY,
              payload TEXT NOT NULL,
              updated_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS sync_state (
              entity_name TEXT PRIMARY KEY,
              last_pull_version INTEGER NOT NULL DEFAULT 0,
              updated_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS app_state (
              state_key TEXT PRIMARY KEY,
              state_value TEXT NOT NULL,
              updated_at TEXT NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task SaveAsync(string key, string payload, CancellationToken ct = default)
    {
        await using var db = new SqliteConnection($"Data Source={DatabasePath}");
        await db.OpenAsync(ct);
        await using var command = db.CreateCommand();
        command.CommandText =
            """
            INSERT INTO cache_entries (cache_key, payload, updated_at)
            VALUES ($key, $payload, $updatedAt)
            ON CONFLICT(cache_key) DO UPDATE SET
              payload = excluded.payload,
              updated_at = excluded.updated_at;
            """;
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$payload", payload);
        command.Parameters.AddWithValue("$updatedAt", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<string?> LoadAsync(string key, CancellationToken ct = default)
    {
        await using var db = new SqliteConnection($"Data Source={DatabasePath}");
        await db.OpenAsync(ct);
        await using var command = db.CreateCommand();
        command.CommandText = "SELECT payload FROM cache_entries WHERE cache_key = $key";
        command.Parameters.AddWithValue("$key", key);
        var result = await command.ExecuteScalarAsync(ct);
        return result as string;
    }

    public async Task SaveStateAsync(string key, string value, CancellationToken ct = default)
    {
        await using var db = new SqliteConnection($"Data Source={DatabasePath}");
        await db.OpenAsync(ct);
        await using var command = db.CreateCommand();
        command.CommandText =
            """
            INSERT INTO app_state (state_key, state_value, updated_at)
            VALUES ($key, $value, $updatedAt)
            ON CONFLICT(state_key) DO UPDATE SET
              state_value = excluded.state_value,
              updated_at = excluded.updated_at;
            """;
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        command.Parameters.AddWithValue("$updatedAt", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<string?> LoadStateAsync(string key, CancellationToken ct = default)
    {
        await using var db = new SqliteConnection($"Data Source={DatabasePath}");
        await db.OpenAsync(ct);
        await using var command = db.CreateCommand();
        command.CommandText = "SELECT state_value FROM app_state WHERE state_key = $key";
        command.Parameters.AddWithValue("$key", key);
        var result = await command.ExecuteScalarAsync(ct);
        return result as string;
    }

    private static string ResolvePath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
            return configuredPath;

        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(root, "Ordevo", configuredPath);
    }
}
