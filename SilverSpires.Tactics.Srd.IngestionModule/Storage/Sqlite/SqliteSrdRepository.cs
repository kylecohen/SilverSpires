using Microsoft.Data.Sqlite;
using System.Text.Json;
using System.Text.Json.Serialization;
using SilverSpires.Tactics.Srd.Ingestion.Abstractions;
using SilverSpires.Tactics.Srd.Ingestion.Storage;
using SilverSpires.Tactics.Srd.Characters;
using SilverSpires.Tactics.Srd.Items;
using SilverSpires.Tactics.Srd.Monsters;
using SilverSpires.Tactics.Srd.Rules;
using SilverSpires.Tactics.Srd.Spells;

namespace SilverSpires.Tactics.Srd.Ingestion.Storage.Sqlite;

public sealed class SqliteSrdRepository : ISrdRepository
{
    private readonly string _dbPath;
    private readonly JsonSerializerOptions _json;

    public SqliteSrdRepository(string dbPath)
    {
        _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));

        _json = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
        _json.Converters.Add(new JsonStringEnumConverter());
    }

    private SqliteConnection CreateConnection() => new($"Data Source={_dbPath}");

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS SrdEntities (
    EntityType TEXT NOT NULL,
    Id TEXT NOT NULL,
    Json TEXT NOT NULL,
    UpdatedUtc TEXT NOT NULL,
    PRIMARY KEY (EntityType, Id)
);

CREATE TABLE IF NOT EXISTS Sources (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    Kind TEXT NOT NULL,
    ConnectionJson TEXT NOT NULL,
    IsEnabled INTEGER NOT NULL,
    UpdatedUtc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS MappingProfiles (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    EntityType TEXT NOT NULL,
    TargetClrType TEXT NOT NULL,
    RulesJson TEXT NOT NULL,
    UpdatedUtc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Feeds (
    Id TEXT PRIMARY KEY,
    SourceId TEXT NOT NULL,
    EntityType TEXT NOT NULL,
    FeedJson TEXT NOT NULL,
    MappingProfileId TEXT NOT NULL,
    IsEnabled INTEGER NOT NULL,
    UpdatedUtc TEXT NOT NULL,
    FOREIGN KEY(SourceId) REFERENCES Sources(Id),
    FOREIGN KEY(MappingProfileId) REFERENCES MappingProfiles(Id)
);

CREATE TABLE IF NOT EXISTS Meta (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);

INSERT OR IGNORE INTO Meta (Key, Value) VALUES ('SchemaVersion', '" + SrdStorageSchema.CurrentVersion + @"');
";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    // Registry upserts
    public Task UpsertSourceAsync(SourceDefinition source, CancellationToken ct = default)
        => UpsertRegistryAsync("Sources", source.Id, source, ct);

    public Task UpsertMappingProfileAsync(MappingProfile profile, CancellationToken ct = default)
        => UpsertRegistryAsync("MappingProfiles", profile.Id, profile, ct);

    public Task UpsertFeedAsync(SourceEntityFeed feed, CancellationToken ct = default)
        => UpsertRegistryAsync("Feeds", feed.Id, feed, ct);

    public async Task<SourceDefinition?> GetSourceAsync(string id, CancellationToken ct = default)
        => await GetRegistryAsync<SourceDefinition>("Sources", id, ct);

    public async Task<MappingProfile?> GetMappingProfileAsync(string id, CancellationToken ct = default)
        => await GetRegistryAsync<MappingProfile>("MappingProfiles", id, ct);

    public async Task<SourceEntityFeed?> GetFeedAsync(string id, CancellationToken ct = default)
        => await GetRegistryAsync<SourceEntityFeed>("Feeds", id, ct);

    public async Task<IReadOnlyList<SourceEntityFeed>> GetEnabledFeedsAsync(string sourceId, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, SourceId, EntityType, FeedJson, MappingProfileId, IsEnabled, UpdatedUtc FROM Feeds WHERE SourceId=$sid AND IsEnabled=1;";
        cmd.Parameters.AddWithValue("$sid", sourceId);

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
                IsEnabled = r.GetInt32(5) == 1,
                UpdatedUtc = DateTime.Parse(r.GetString(6))
            });
        }
        return list;
    }

    private async Task UpsertRegistryAsync<T>(string table, string id, T entity, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        // serialize entity, but write to specific table columns for query friendliness
        // We'll handle each registry table explicitly:
        if (entity is SourceDefinition s)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
INSERT INTO Sources (Id, Name, Kind, ConnectionJson, IsEnabled, UpdatedUtc)
VALUES ($id, $name, $kind, $conn, $enabled, $utc)
ON CONFLICT(Id) DO UPDATE SET
    Name=excluded.Name,
    Kind=excluded.Kind,
    ConnectionJson=excluded.ConnectionJson,
    IsEnabled=excluded.IsEnabled,
    UpdatedUtc=excluded.UpdatedUtc;";
            cmd.Parameters.AddWithValue("$id", s.Id);
            cmd.Parameters.AddWithValue("$name", s.Name);
            cmd.Parameters.AddWithValue("$kind", s.Kind.ToString());
            cmd.Parameters.AddWithValue("$conn", s.ConnectionJson);
            cmd.Parameters.AddWithValue("$enabled", s.IsEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("$utc", DateTime.UtcNow.ToString("o"));
            await cmd.ExecuteNonQueryAsync(ct);
            return;
        }

        if (entity is MappingProfile p)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
INSERT INTO MappingProfiles (Id, Name, EntityType, TargetClrType, RulesJson, UpdatedUtc)
VALUES ($id, $name, $etype, $clr, $rules, $utc)
ON CONFLICT(Id) DO UPDATE SET
    Name=excluded.Name,
    EntityType=excluded.EntityType,
    TargetClrType=excluded.TargetClrType,
    RulesJson=excluded.RulesJson,
    UpdatedUtc=excluded.UpdatedUtc;";
            cmd.Parameters.AddWithValue("$id", p.Id);
            cmd.Parameters.AddWithValue("$name", p.Name);
            cmd.Parameters.AddWithValue("$etype", p.EntityType.ToString());
            cmd.Parameters.AddWithValue("$clr", p.TargetClrType ?? "");
            cmd.Parameters.AddWithValue("$rules", p.RulesJson ?? "{}");
            cmd.Parameters.AddWithValue("$utc", DateTime.UtcNow.ToString("o"));
            await cmd.ExecuteNonQueryAsync(ct);
            return;
        }

        if (entity is SourceEntityFeed f)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
INSERT INTO Feeds (Id, SourceId, EntityType, FeedJson, MappingProfileId, IsEnabled, UpdatedUtc)
VALUES ($id, $sid, $etype, $feed, $mpid, $enabled, $utc)
ON CONFLICT(Id) DO UPDATE SET
    SourceId=excluded.SourceId,
    EntityType=excluded.EntityType,
    FeedJson=excluded.FeedJson,
    MappingProfileId=excluded.MappingProfileId,
    IsEnabled=excluded.IsEnabled,
    UpdatedUtc=excluded.UpdatedUtc;";
            cmd.Parameters.AddWithValue("$id", f.Id);
            cmd.Parameters.AddWithValue("$sid", f.SourceId);
            cmd.Parameters.AddWithValue("$etype", f.EntityType.ToString());
            cmd.Parameters.AddWithValue("$feed", f.FeedJson ?? "{}");
            cmd.Parameters.AddWithValue("$mpid", f.MappingProfileId);
            cmd.Parameters.AddWithValue("$enabled", f.IsEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("$utc", DateTime.UtcNow.ToString("o"));
            await cmd.ExecuteNonQueryAsync(ct);
            return;
        }

        throw new NotSupportedException($"Unknown registry entity type: {typeof(T).Name}");
    }

    private async Task<T?> GetRegistryAsync<T>(string table, string id, CancellationToken ct) where T : class
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        if (table == "Sources")
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Kind, ConnectionJson, IsEnabled, UpdatedUtc FROM Sources WHERE Id=$id;";
            cmd.Parameters.AddWithValue("$id", id);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct)) return null;

            return new SourceDefinition
            {
                Id = r.GetString(0),
                Name = r.GetString(1),
                Kind = Enum.Parse<SrdSourceKind>(r.GetString(2)),
                ConnectionJson = r.GetString(3),
                IsEnabled = r.GetInt32(4) == 1,
                UpdatedUtc = DateTime.Parse(r.GetString(5))
            } as T;
        }

        if (table == "MappingProfiles")
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, EntityType, TargetClrType, RulesJson, UpdatedUtc FROM MappingProfiles WHERE Id=$id;";
            cmd.Parameters.AddWithValue("$id", id);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct)) return null;

            return new MappingProfile
            {
                Id = r.GetString(0),
                Name = r.GetString(1),
                EntityType = Enum.Parse<SrdEntityType>(r.GetString(2)),
                TargetClrType = r.GetString(3),
                RulesJson = r.GetString(4),
                UpdatedUtc = DateTime.Parse(r.GetString(5))
            } as T;
        }

        if (table == "Feeds")
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, SourceId, EntityType, FeedJson, MappingProfileId, IsEnabled, UpdatedUtc FROM Feeds WHERE Id=$id;";
            cmd.Parameters.AddWithValue("$id", id);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct)) return null;

            return new SourceEntityFeed
            {
                Id = r.GetString(0),
                SourceId = r.GetString(1),
                EntityType = Enum.Parse<SrdEntityType>(r.GetString(2)),
                FeedJson = r.GetString(3),
                MappingProfileId = r.GetString(4),
                IsEnabled = r.GetInt32(5) == 1,
                UpdatedUtc = DateTime.Parse(r.GetString(6))
            } as T;
        }

        return null;
    }

    // SRD entity upserts
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
INSERT INTO SrdEntities (EntityType, Id, Json, UpdatedUtc)
VALUES ($type, $id, $json, $utc)
ON CONFLICT(EntityType, Id) DO UPDATE SET
    Json=excluded.Json,
    UpdatedUtc=excluded.UpdatedUtc;";
        cmd.Parameters.AddWithValue("$type", type);
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$json", json);
        cmd.Parameters.AddWithValue("$utc", DateTime.UtcNow.ToString("o"));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    // SRD entity reads
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
        cmd.CommandText = "SELECT Json FROM SrdEntities WHERE EntityType=$type;";
        cmd.Parameters.AddWithValue("$type", type);

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
