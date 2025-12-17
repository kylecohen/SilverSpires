using Microsoft.Data.SqlClient;
using SilverSpires.Tactics.Srd.Characters;
using SilverSpires.Tactics.Srd.Items;
using SilverSpires.Tactics.Srd.Monsters;
using SilverSpires.Tactics.Srd.Persistence.Registry;
using SilverSpires.Tactics.Srd.Persistence.Storage.Json;
using SilverSpires.Tactics.Srd.Rules;
using SilverSpires.Tactics.Srd.Spells;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SilverSpires.Tactics.Srd.Persistence.Storage.SqlServer;

public sealed class SqlServerSrdRepository : ISrdRepository
{
    private readonly string _cs;
    private readonly JsonSerializerOptions _json;

    public SqlServerSrdRepository(string connectionString)
    {
        _cs = connectionString ?? throw new ArgumentNullException(nameof(connectionString));

        _json = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        _json.Converters.Add(new JsonStringEnumConverter());
        _json.Converters.Add(new ChallengeRatingJsonConverter());
    }

    private SqlConnection CreateConnection() => new(_cs);

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
IF OBJECT_ID('dbo.SrdEntities','U') IS NULL
BEGIN
  CREATE TABLE dbo.SrdEntities(
    EntityType NVARCHAR(64) NOT NULL,
    Id NVARCHAR(256) NOT NULL,
    Json NVARCHAR(MAX) NOT NULL,
    UpdatedUtc DATETIME2 NOT NULL,
    CONSTRAINT PK_SrdEntities PRIMARY KEY (EntityType, Id)
  );
END

IF OBJECT_ID('dbo.Sources','U') IS NULL
BEGIN
  CREATE TABLE dbo.Sources(
    Id NVARCHAR(128) NOT NULL PRIMARY KEY,
    Name NVARCHAR(256) NOT NULL,
    Kind NVARCHAR(64) NOT NULL,
    ConnectionJson NVARCHAR(MAX) NOT NULL,
    IsEnabled BIT NOT NULL,
    UpdatedUtc DATETIME2 NOT NULL
  );
END

IF OBJECT_ID('dbo.MappingProfiles','U') IS NULL
BEGIN
  CREATE TABLE dbo.MappingProfiles(
    Id NVARCHAR(128) NOT NULL PRIMARY KEY,
    Name NVARCHAR(256) NOT NULL,
    EntityType NVARCHAR(64) NOT NULL,
    RulesJson NVARCHAR(MAX) NOT NULL,
    UpdatedUtc DATETIME2 NOT NULL
  );
END

IF OBJECT_ID('dbo.Feeds','U') IS NULL
BEGIN
  CREATE TABLE dbo.Feeds(
    Id NVARCHAR(128) NOT NULL PRIMARY KEY,
    SourceId NVARCHAR(128) NOT NULL,
    EntityType NVARCHAR(64) NOT NULL,
    FeedJson NVARCHAR(MAX) NOT NULL,
    MappingProfileId NVARCHAR(128) NOT NULL,
    IsEnabled BIT NOT NULL,
    UpdatedUtc DATETIME2 NOT NULL
  );
END
";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    // Registry
    public Task UpsertSourceAsync(SourceDefinition source, CancellationToken ct = default) => UpsertSourceInternalAsync(source, ct);
    public Task UpsertMappingProfileAsync(MappingProfile profile, CancellationToken ct = default) => UpsertProfileInternalAsync(profile, ct);
    public Task UpsertFeedAsync(SourceEntityFeed feed, CancellationToken ct = default) => UpsertFeedInternalAsync(feed, ct);

    public async Task<IReadOnlyList<SourceDefinition>> GetSourcesAsync(CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Kind, ConnectionJson, IsEnabled, UpdatedUtc FROM dbo.Sources ORDER BY Name;";

        var list = new List<SourceDefinition>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            list.Add(new SourceDefinition
            {
                Id = r.GetString(0),
                Name = r.GetString(1),
                Kind = Enum.Parse<SrdSourceKind>(r.GetString(2)),
                ConnectionJson = r.GetString(3),
                IsEnabled = r.GetBoolean(4),
                UpdatedUtc = r.GetDateTime(5)
            });
        }
        return list;
    }

    public async Task<SourceDefinition?> GetSourceAsync(string id, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Kind, ConnectionJson, IsEnabled, UpdatedUtc FROM dbo.Sources WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;

        return new SourceDefinition
        {
            Id = r.GetString(0),
            Name = r.GetString(1),
            Kind = Enum.Parse<SrdSourceKind>(r.GetString(2)),
            ConnectionJson = r.GetString(3),
            IsEnabled = r.GetBoolean(4),
            UpdatedUtc = r.GetDateTime(5)
        };
    }

    public async Task<IReadOnlyList<SourceEntityFeed>> GetFeedsBySourceAsync(string sourceId, bool enabledOnly, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = enabledOnly
            ? "SELECT Id, SourceId, EntityType, FeedJson, MappingProfileId, IsEnabled, UpdatedUtc FROM dbo.Feeds WHERE SourceId=@sid AND IsEnabled=1"
            : "SELECT Id, SourceId, EntityType, FeedJson, MappingProfileId, IsEnabled, UpdatedUtc FROM dbo.Feeds WHERE SourceId=@sid";
        cmd.Parameters.AddWithValue("@sid", sourceId);

        var list = new List<SourceEntityFeed>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            list.Add(new SourceEntityFeed
            {
                Id = r.GetString(0),
                SourceId = r.GetString(1),
                EntityType = Enum.Parse<SrdEntityType>(r.GetString(2)),
                FeedJson = r.GetString(3),
                MappingProfileId = r.GetString(4),
                IsEnabled = r.GetBoolean(5),
                UpdatedUtc = r.GetDateTime(6)
            });
        }
        return list;
    }

    public async Task<MappingProfile?> GetMappingProfileAsync(string id, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, EntityType, RulesJson, UpdatedUtc FROM dbo.MappingProfiles WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;

        return new MappingProfile
        {
            Id = r.GetString(0),
            Name = r.GetString(1),
            EntityType = Enum.Parse<SrdEntityType>(r.GetString(2)),
            RulesJson = r.GetString(3),
            UpdatedUtc = r.GetDateTime(4)
        };
    }

    private async Task UpsertSourceInternalAsync(SourceDefinition s, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
MERGE dbo.Sources AS tgt
USING (SELECT @id AS Id) AS src
ON tgt.Id = src.Id
WHEN MATCHED THEN UPDATE SET
  Name=@name, Kind=@kind, ConnectionJson=@conn, IsEnabled=@enabled, UpdatedUtc=SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT (Id, Name, Kind, ConnectionJson, IsEnabled, UpdatedUtc)
VALUES (@id, @name, @kind, @conn, @enabled, SYSUTCDATETIME());";
        cmd.Parameters.AddWithValue("@id", s.Id);
        cmd.Parameters.AddWithValue("@name", s.Name);
        cmd.Parameters.AddWithValue("@kind", s.Kind.ToString());
        cmd.Parameters.AddWithValue("@conn", s.ConnectionJson);
        cmd.Parameters.AddWithValue("@enabled", s.IsEnabled);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task UpsertProfileInternalAsync(MappingProfile p, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
MERGE dbo.MappingProfiles AS tgt
USING (SELECT @id AS Id) AS src
ON tgt.Id = src.Id
WHEN MATCHED THEN UPDATE SET
  Name=@name, EntityType=@etype, RulesJson=@rules, UpdatedUtc=SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT (Id, Name, EntityType, RulesJson, UpdatedUtc)
VALUES (@id, @name, @etype, @rules, SYSUTCDATETIME());";
        cmd.Parameters.AddWithValue("@id", p.Id);
        cmd.Parameters.AddWithValue("@name", p.Name);
        cmd.Parameters.AddWithValue("@etype", p.EntityType.ToString());
        cmd.Parameters.AddWithValue("@rules", p.RulesJson);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task UpsertFeedInternalAsync(SourceEntityFeed f, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
MERGE dbo.Feeds AS tgt
USING (SELECT @id AS Id) AS src
ON tgt.Id = src.Id
WHEN MATCHED THEN UPDATE SET
  SourceId=@sid, EntityType=@etype, FeedJson=@feed, MappingProfileId=@mpid, IsEnabled=@enabled, UpdatedUtc=SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT (Id, SourceId, EntityType, FeedJson, MappingProfileId, IsEnabled, UpdatedUtc)
VALUES (@id, @sid, @etype, @feed, @mpid, @enabled, SYSUTCDATETIME());";
        cmd.Parameters.AddWithValue("@id", f.Id);
        cmd.Parameters.AddWithValue("@sid", f.SourceId);
        cmd.Parameters.AddWithValue("@etype", f.EntityType.ToString());
        cmd.Parameters.AddWithValue("@feed", f.FeedJson);
        cmd.Parameters.AddWithValue("@mpid", f.MappingProfileId);
        cmd.Parameters.AddWithValue("@enabled", f.IsEnabled);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    // Entity upserts
    public Task UpsertClassAsync(SrdClass entity, CancellationToken ct = default) => UpsertEntityAsync("Class", entity.Id, entity, ct);
    public Task UpsertRaceAsync(SrdRace entity, CancellationToken ct = default) => UpsertEntityAsync("Race", entity.Id, entity, ct);
    public Task UpsertBackgroundAsync(SrdBackground entity, CancellationToken ct = default) => UpsertEntityAsync("Background", entity.Id, entity, ct);
    public Task UpsertFeatAsync(SrdFeat entity, CancellationToken ct = default) => UpsertEntityAsync("Feat", entity.Id, entity, ct);
    public Task UpsertSkillAsync(SrdSkill entity, CancellationToken ct = default) => UpsertEntityAsync("Skill", entity.Id, entity, ct);
    public Task UpsertLanguageAsync(SrdLanguage entity, CancellationToken ct = default) => UpsertEntityAsync("Language", entity.Id, entity, ct);
    public Task UpsertSpellAsync(SrdSpell entity, CancellationToken ct = default) => UpsertEntityAsync("Spell", entity.Id, entity, ct);
    public Task UpsertMonsterAsync(SrdMonster entity, CancellationToken ct = default) => UpsertEntityAsync("Monster", entity.Id, entity, ct);
    public Task UpsertMagicItemAsync(SrdMagicItem entity, CancellationToken ct = default) => UpsertEntityAsync("MagicItem", entity.Id, entity, ct);
    public Task UpsertEquipmentAsync(SrdEquipment entity, CancellationToken ct = default) => UpsertEntityAsync("Equipment", entity.Id, entity, ct);
    public Task UpsertWeaponAsync(SrdWeapon entity, CancellationToken ct = default) => UpsertEntityAsync("Weapon", entity.Id, entity, ct);
    public Task UpsertArmorAsync(SrdArmor entity, CancellationToken ct = default) => UpsertEntityAsync("Armor", entity.Id, entity, ct);
    public Task UpsertEffectAsync(GameEffect entity, CancellationToken ct = default) => UpsertEntityAsync("Effect", entity.Id, entity, ct);

    private async Task UpsertEntityAsync<T>(string type, string id, T entity, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        var json = JsonSerializer.Serialize(entity, _json);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
MERGE dbo.SrdEntities AS tgt
USING (SELECT @type AS EntityType, @id AS Id) AS src
ON tgt.EntityType = src.EntityType AND tgt.Id = src.Id
WHEN MATCHED THEN UPDATE SET Json=@json, UpdatedUtc=SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT (EntityType, Id, Json, UpdatedUtc) VALUES (@type, @id, @json, SYSUTCDATETIME());";
        cmd.Parameters.AddWithValue("@type", type);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@json", json);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    // Entity reads
    public Task<IReadOnlyList<SrdClass>> GetAllClassesAsync(CancellationToken ct = default) => GetAllAsync<SrdClass>("Class", ct);
    public Task<IReadOnlyList<SrdRace>> GetAllRacesAsync(CancellationToken ct = default) => GetAllAsync<SrdRace>("Race", ct);
    public Task<IReadOnlyList<SrdBackground>> GetAllBackgroundsAsync(CancellationToken ct = default) => GetAllAsync<SrdBackground>("Background", ct);
    public Task<IReadOnlyList<SrdFeat>> GetAllFeatsAsync(CancellationToken ct = default) => GetAllAsync<SrdFeat>("Feat", ct);
    public Task<IReadOnlyList<SrdSkill>> GetAllSkillsAsync(CancellationToken ct = default) => GetAllAsync<SrdSkill>("Skill", ct);
    public Task<IReadOnlyList<SrdLanguage>> GetAllLanguagesAsync(CancellationToken ct = default) => GetAllAsync<SrdLanguage>("Language", ct);
    public Task<IReadOnlyList<SrdSpell>> GetAllSpellsAsync(CancellationToken ct = default) => GetAllAsync<SrdSpell>("Spell", ct);
    public Task<IReadOnlyList<SrdMonster>> GetAllMonstersAsync(CancellationToken ct = default) => GetAllAsync<SrdMonster>("Monster", ct);
    public Task<IReadOnlyList<SrdMagicItem>> GetAllMagicItemsAsync(CancellationToken ct = default) => GetAllAsync<SrdMagicItem>("MagicItem", ct);
    public Task<IReadOnlyList<SrdEquipment>> GetAllEquipmentAsync(CancellationToken ct = default) => GetAllAsync<SrdEquipment>("Equipment", ct);
    public Task<IReadOnlyList<SrdWeapon>> GetAllWeaponsAsync(CancellationToken ct = default) => GetAllAsync<SrdWeapon>("Weapon", ct);
    public Task<IReadOnlyList<SrdArmor>> GetAllArmorAsync(CancellationToken ct = default) => GetAllAsync<SrdArmor>("Armor", ct);
    public Task<IReadOnlyList<GameEffect>> GetAllEffectsAsync(CancellationToken ct = default) => GetAllAsync<GameEffect>("Effect", ct);

    private async Task<IReadOnlyList<T>> GetAllAsync<T>(string type, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Json FROM dbo.SrdEntities WHERE EntityType=@type";
        cmd.Parameters.AddWithValue("@type", type);

        var list = new List<T>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            var json = r.GetString(0);
            var entity = JsonSerializer.Deserialize<T>(json, _json);
            if (entity != null) list.Add(entity);
        }
        return list;
    }
}
