using Microsoft.Data.Sqlite;
using SilverSpires.Tactics.Srd.Characters;
using SilverSpires.Tactics.Srd.Items;
using SilverSpires.Tactics.Srd.Monsters;
using SilverSpires.Tactics.Srd.Persistence.Registry;
using SilverSpires.Tactics.Srd.Persistence.Storage.Json;
using SilverSpires.Tactics.Srd.Persistence.Storage.SqlServer;
using SilverSpires.Tactics.Srd.Rules;
using SilverSpires.Tactics.Srd.Spells;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SilverSpires.Tactics.Srd.Persistence.Storage.Sqlite;

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
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        _json.Converters.Add(new JsonStringEnumConverter());
        _json.Converters.Add(new ChallengeRatingJsonConverter());
    }

    private SqliteConnection CreateConnection() => new($"Data Source={_dbPath}");

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
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
  UpdatedUtc TEXT NOT NULL
);
";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    // Registry
    public async Task UpsertSourceAsync(SourceDefinition source, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO Sources (Id, Name, Kind, ConnectionJson, IsEnabled, UpdatedUtc)
VALUES ($id,$name,$kind,$conn,$enabled,$utc)
ON CONFLICT(Id) DO UPDATE SET
  Name=excluded.Name,
  Kind=excluded.Kind,
  ConnectionJson=excluded.ConnectionJson,
  IsEnabled=excluded.IsEnabled,
  UpdatedUtc=excluded.UpdatedUtc;";
        cmd.Parameters.AddWithValue("$id", source.Id);
        cmd.Parameters.AddWithValue("$name", source.Name);
        cmd.Parameters.AddWithValue("$kind", source.Kind.ToString());
        cmd.Parameters.AddWithValue("$conn", source.ConnectionJson);
        cmd.Parameters.AddWithValue("$enabled", source.IsEnabled ? 1 : 0);
        cmd.Parameters.AddWithValue("$utc", DateTime.UtcNow.ToString("o"));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpsertMappingProfileAsync(MappingProfile profile, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO MappingProfiles (Id, Name, EntityType, RulesJson, UpdatedUtc)
VALUES ($id,$name,$etype,$rules,$utc)
ON CONFLICT(Id) DO UPDATE SET
  Name=excluded.Name,
  EntityType=excluded.EntityType,
  RulesJson=excluded.RulesJson,
  UpdatedUtc=excluded.UpdatedUtc;";
        cmd.Parameters.AddWithValue("$id", profile.Id);
        cmd.Parameters.AddWithValue("$name", profile.Name);
        cmd.Parameters.AddWithValue("$etype", profile.EntityType.ToString());
        cmd.Parameters.AddWithValue("$rules", profile.RulesJson);
        cmd.Parameters.AddWithValue("$utc", DateTime.UtcNow.ToString("o"));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpsertFeedAsync(SourceEntityFeed feed, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO Feeds (Id, SourceId, EntityType, FeedJson, MappingProfileId, IsEnabled, UpdatedUtc)
VALUES ($id,$sid,$etype,$feed,$mpid,$enabled,$utc)
ON CONFLICT(Id) DO UPDATE SET
  SourceId=excluded.SourceId,
  EntityType=excluded.EntityType,
  FeedJson=excluded.FeedJson,
  MappingProfileId=excluded.MappingProfileId,
  IsEnabled=excluded.IsEnabled,
  UpdatedUtc=excluded.UpdatedUtc;";
        cmd.Parameters.AddWithValue("$id", feed.Id);
        cmd.Parameters.AddWithValue("$sid", feed.SourceId);
        cmd.Parameters.AddWithValue("$etype", feed.EntityType.ToString());
        cmd.Parameters.AddWithValue("$feed", feed.FeedJson);
        cmd.Parameters.AddWithValue("$mpid", feed.MappingProfileId);
        cmd.Parameters.AddWithValue("$enabled", feed.IsEnabled ? 1 : 0);
        cmd.Parameters.AddWithValue("$utc", DateTime.UtcNow.ToString("o"));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<SourceDefinition>> GetSourcesAsync(CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Kind, ConnectionJson, IsEnabled, UpdatedUtc FROM Sources ORDER BY Name;";

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
                IsEnabled = r.GetInt32(4) == 1,
                UpdatedUtc = DateTime.Parse(r.GetString(5))
            });
        }
        return list;
    }

    public async Task<SourceDefinition?> GetSourceAsync(string id, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

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
        };
    }

    public async Task<IReadOnlyList<SourceEntityFeed>> GetFeedsBySourceAsync(string sourceId, bool enabledOnly, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = enabledOnly
            ? "SELECT Id, SourceId, EntityType, FeedJson, MappingProfileId, IsEnabled, UpdatedUtc FROM Feeds WHERE SourceId=$sid AND IsEnabled=1;"
            : "SELECT Id, SourceId, EntityType, FeedJson, MappingProfileId, IsEnabled, UpdatedUtc FROM Feeds WHERE SourceId=$sid;";
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

    public async Task<MappingProfile?> GetMappingProfileAsync(string id, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, EntityType, RulesJson, UpdatedUtc FROM MappingProfiles WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", id);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;

        return new MappingProfile
        {
            Id = r.GetString(0),
            Name = r.GetString(1),
            EntityType = Enum.Parse<SrdEntityType>(r.GetString(2)),
            RulesJson = r.GetString(3),
            UpdatedUtc = DateTime.Parse(r.GetString(4))
        };
    }

    // Entity upsert helpers
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
VALUES ($type,$id,$json,$utc)
ON CONFLICT(EntityType, Id) DO UPDATE SET
  Json=excluded.Json,
  UpdatedUtc=excluded.UpdatedUtc;";
        cmd.Parameters.AddWithValue("$type", type);
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$json", json);
        cmd.Parameters.AddWithValue("$utc", DateTime.UtcNow.ToString("o"));
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
        cmd.CommandText = "SELECT Id, Json FROM SrdEntities WHERE EntityType=$type;";
        cmd.Parameters.AddWithValue("$type", type);

        var list = new List<T>();
        await using var r = await cmd.ExecuteReaderAsync(ct);

        while (await r.ReadAsync(ct))
        {
            var id = r.GetString(0);
            var json = r.GetString(1);

            try
            {
                var entity = JsonSerializer.Deserialize<T>(json, _json);
                if (entity != null) list.Add(entity);
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"[SRD DESERIALIZE FAIL] EntityType={type} Id={id} :: {ex.Message}");
            }
        }

        return list;
    }

    // -------------------------
    // Sync helpers (raw)
    // -------------------------
    public async Task<IReadOnlyList<SrdEntityEnvelope>> GetEntityBatchAsync(
        string entityType,
        DateTime? updatedSinceUtc,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page < 0) page = 0;
        if (pageSize <= 0) pageSize = 100;
        if (pageSize > 2000) pageSize = 2000;

        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT EntityType, Id, Json, UpdatedUtc
FROM SrdEntities
WHERE EntityType=$type
  AND ($since IS NULL OR UpdatedUtc > $since)
ORDER BY UpdatedUtc ASC, Id ASC
LIMIT $take OFFSET $skip;";
        cmd.Parameters.AddWithValue("$type", entityType);
        cmd.Parameters.AddWithValue("$since", updatedSinceUtc is null ? (object?)DBNull.Value : updatedSinceUtc.Value.ToString("o"));
        cmd.Parameters.AddWithValue("$take", pageSize);
        cmd.Parameters.AddWithValue("$skip", page * pageSize);

        var list = new List<SrdEntityEnvelope>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            var et = r.GetString(0);
            var id = r.GetString(1);
            var json = r.GetString(2);
            var utcStr = r.GetString(3);

            DateTime utc;
            if (!DateTime.TryParse(utcStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out utc))
                utc = DateTime.UtcNow;

            list.Add(new SrdEntityEnvelope(et, id, json, utc));
        }

        return list;
    }

    public async Task<DateTime?> GetLatestEntityUpdatedUtcAsync(CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT MAX(UpdatedUtc) FROM SrdEntities;";
        var scalar = await cmd.ExecuteScalarAsync(ct);
        if (scalar is null || scalar is DBNull) return null;

        var s = scalar.ToString();
        if (string.IsNullOrWhiteSpace(s)) return null;

        if (DateTime.TryParse(s, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
            return dt;

        return null;
    }

}
