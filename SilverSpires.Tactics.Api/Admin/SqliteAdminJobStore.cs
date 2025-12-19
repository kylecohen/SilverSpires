using Microsoft.Data.Sqlite;

namespace SilverSpires.Tactics.Api.Admin;

public sealed class SqliteAdminJobStore : IAdminJobStore
{
    private readonly string _dbPath;

    public SqliteAdminJobStore(string dbPath)
    {
        _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS AdminJobs (
  Id TEXT PRIMARY KEY,
  Type TEXT NOT NULL,
  State TEXT NOT NULL,
  CreatedUtc TEXT NOT NULL,
  StartedUtc TEXT NULL,
  CompletedUtc TEXT NULL,
  Error TEXT NULL
);";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<AdminJob> CreateQueuedAsync(string type, CancellationToken ct = default)
    {
        var job = new AdminJob(Guid.NewGuid(), type, "queued", DateTime.UtcNow, null, null, null);

        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO AdminJobs (Id, Type, State, CreatedUtc) VALUES ($id,$t,$s,$c);";
        cmd.Parameters.AddWithValue("$id", job.Id.ToString());
        cmd.Parameters.AddWithValue("$t", job.Type);
        cmd.Parameters.AddWithValue("$s", job.State);
        cmd.Parameters.AddWithValue("$c", job.CreatedUtc.ToString("o"));
        await cmd.ExecuteNonQueryAsync(ct);

        return job;
    }

    public async Task<AdminJob?> GetAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT Id, Type, State, CreatedUtc, StartedUtc, CompletedUtc, Error FROM AdminJobs WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", id.ToString());

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;

        return new AdminJob(
            Guid.Parse(r.GetString(0)),
            r.GetString(1),
            r.GetString(2),
            DateTime.Parse(r.GetString(3), null, System.Globalization.DateTimeStyles.RoundtripKind),
            r.IsDBNull(4) ? null : DateTime.Parse(r.GetString(4), null, System.Globalization.DateTimeStyles.RoundtripKind),
            r.IsDBNull(5) ? null : DateTime.Parse(r.GetString(5), null, System.Globalization.DateTimeStyles.RoundtripKind),
            r.IsDBNull(6) ? null : r.GetString(6));
    }

    public async Task UpdateStateAsync(Guid id, string state, DateTime? startedUtc = null, DateTime? completedUtc = null, string? error = null, CancellationToken ct = default)
    {
        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
UPDATE AdminJobs
SET State=$s,
    StartedUtc=COALESCE($st, StartedUtc),
    CompletedUtc=COALESCE($co, CompletedUtc),
    Error=COALESCE($e, Error)
WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.Parameters.AddWithValue("$s", state);
        cmd.Parameters.AddWithValue("$st", (object?)startedUtc?.ToString("o") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$co", (object?)completedUtc?.ToString("o") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$e", (object?)error ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
