using Microsoft.Data.SqlClient;

namespace SilverSpires.Tactics.Api.Admin;

public sealed class SqlServerAdminJobStore : IAdminJobStore
{
    private readonly string _connectionString;

    public SqlServerAdminJobStore(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
IF OBJECT_ID('dbo.AdminJobs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AdminJobs (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Type NVARCHAR(64) NOT NULL,
        State NVARCHAR(32) NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        StartedUtc DATETIME2 NULL,
        CompletedUtc DATETIME2 NULL,
        Error NVARCHAR(MAX) NULL
    );
END";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<AdminJob> CreateQueuedAsync(string type, CancellationToken ct = default)
    {
        var job = new AdminJob(Guid.NewGuid(), type, "queued", DateTime.UtcNow, null, null, null);

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO dbo.AdminJobs (Id, Type, State, CreatedUtc) VALUES (@id,@t,@s,@c);";
        cmd.Parameters.AddWithValue("@id", job.Id);
        cmd.Parameters.AddWithValue("@t", job.Type);
        cmd.Parameters.AddWithValue("@s", job.State);
        cmd.Parameters.AddWithValue("@c", job.CreatedUtc);
        await cmd.ExecuteNonQueryAsync(ct);

        return job;
    }

    public async Task<AdminJob?> GetAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT Id, Type, State, CreatedUtc, StartedUtc, CompletedUtc, Error FROM dbo.AdminJobs WHERE Id=@id;";
        cmd.Parameters.AddWithValue("@id", id);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;

        return new AdminJob(
            r.GetGuid(0),
            r.GetString(1),
            r.GetString(2),
            r.GetDateTime(3),
            r.IsDBNull(4) ? null : r.GetDateTime(4),
            r.IsDBNull(5) ? null : r.GetDateTime(5),
            r.IsDBNull(6) ? null : r.GetString(6));
    }

    public async Task UpdateStateAsync(Guid id, string state, DateTime? startedUtc = null, DateTime? completedUtc = null, string? error = null, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
UPDATE dbo.AdminJobs
SET State=@s,
    StartedUtc=COALESCE(@st, StartedUtc),
    CompletedUtc=COALESCE(@co, CompletedUtc),
    Error=COALESCE(@e, Error)
WHERE Id=@id;";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@s", state);
        cmd.Parameters.AddWithValue("@st", (object?)startedUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@co", (object?)completedUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@e", (object?)error ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
