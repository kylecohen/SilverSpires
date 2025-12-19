using Microsoft.Data.Sqlite;
using SilverSpires.Tactics.Factions;

namespace SilverSpires.Tactics.Game;

public sealed class SqliteGameRepository : IGameRepository, IFactionRepository
{
    private readonly string _dbPath;

    public SqliteGameRepository(string dbPath)
    {
        _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        // Core entities
        await ExecAsync(conn, @"
CREATE TABLE IF NOT EXISTS Campaigns (
  Id TEXT PRIMARY KEY,
  Name TEXT NOT NULL,
  Description TEXT NULL,
  CreatedUtc TEXT NOT NULL,
  UpdatedUtc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Characters (
  Id TEXT PRIMARY KEY,
  Name TEXT NOT NULL,
  Notes TEXT NULL,
  ClassId TEXT NULL,
  RaceId TEXT NULL,
  Level INTEGER NOT NULL,
  Strength INTEGER NOT NULL,
  Dexterity INTEGER NOT NULL,
  Constitution INTEGER NOT NULL,
  Intelligence INTEGER NOT NULL,
  Wisdom INTEGER NOT NULL,
  Charisma INTEGER NOT NULL,
  ArmorId TEXT NOT NULL,
  WeaponId TEXT NOT NULL,
  CreatedUtc TEXT NOT NULL,
  UpdatedUtc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Encounters (
  Id TEXT PRIMARY KEY,
  Name TEXT NOT NULL,
  Notes TEXT NULL,
  CreatedUtc TEXT NOT NULL,
  UpdatedUtc TEXT NOT NULL
);

-- Relations
CREATE TABLE IF NOT EXISTS CampaignCharacters (
  CampaignId TEXT NOT NULL,
  CharacterId TEXT NOT NULL,
  PRIMARY KEY (CampaignId, CharacterId)
);

CREATE TABLE IF NOT EXISTS CampaignEncounters (
  CampaignId TEXT NOT NULL,
  EncounterId TEXT NOT NULL,
  PRIMARY KEY (CampaignId, EncounterId)
);

CREATE TABLE IF NOT EXISTS EncounterMonsters (
  EncounterId TEXT NOT NULL,
  MonsterId TEXT NOT NULL,
  Count INTEGER NOT NULL,
  PRIMARY KEY (EncounterId, MonsterId)
);

CREATE TABLE IF NOT EXISTS EncounterCharacters (
  EncounterId TEXT NOT NULL,
  CharacterId TEXT NOT NULL,
  PRIMARY KEY (EncounterId, CharacterId)
);

CREATE TABLE IF NOT EXISTS Settings (
  [Key] TEXT PRIMARY KEY,
  [Value] TEXT NOT NULL
);


-- -----------------
-- Factions + Relationships
-- -----------------
CREATE TABLE IF NOT EXISTS Factions (
  Id TEXT PRIMARY KEY,
  Name TEXT NOT NULL,
  InsigniaRef TEXT NULL,
  CreatedUtc TEXT NOT NULL,
  UpdatedUtc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS FactionRelations (
  SourceFactionId TEXT NOT NULL,
  TargetFactionId TEXT NOT NULL,
  Score INTEGER NOT NULL,
  PRIMARY KEY (SourceFactionId, TargetFactionId)
);

CREATE TABLE IF NOT EXISTS CharacterFactions (
  CharacterId TEXT NOT NULL,
  FactionId TEXT NOT NULL,
  IsPrimary INTEGER NOT NULL,
  Rank INTEGER NOT NULL,
  Title TEXT NULL,
  PRIMARY KEY (CharacterId, FactionId)
);

CREATE TABLE IF NOT EXISTS CharacterPersonalRelations (
  FromCharacterId TEXT NOT NULL,
  ToCharacterId TEXT NOT NULL,
  Score INTEGER NOT NULL,
  PRIMARY KEY (FromCharacterId, ToCharacterId)
);
", ct);
    }

    // -----------------
    // Campaigns
    // -----------------
    public async Task<IReadOnlyList<CampaignRecord>> ListCampaignsAsync(CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Description, CreatedUtc, UpdatedUtc FROM Campaigns ORDER BY UpdatedUtc DESC;";
        var list = new List<CampaignRecord>();

        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            list.Add(ReadCampaign(r));

        return list;
    }

    public async Task<CampaignRecord?> GetCampaignAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Description, CreatedUtc, UpdatedUtc FROM Campaigns WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", id.ToString());

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        return ReadCampaign(r);
    }

    public async Task<CampaignRecord> CreateCampaignAsync(string name, string? description, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var rec = new CampaignRecord(Guid.NewGuid(), name, description, now, now);

        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Campaigns (Id, Name, Description, CreatedUtc, UpdatedUtc)
VALUES ($id,$name,$desc,$c,$u);";
        cmd.Parameters.AddWithValue("$id", rec.Id.ToString());
        cmd.Parameters.AddWithValue("$name", rec.Name);
        cmd.Parameters.AddWithValue("$desc", (object?)rec.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$u", rec.UpdatedUtc.ToString("o"));
        await cmd.ExecuteNonQueryAsync(ct);

        return rec;
    }

    public async Task UpdateCampaignAsync(CampaignRecord campaign, CancellationToken ct = default)
    {
        var rec = campaign with { UpdatedUtc = DateTime.UtcNow };

        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE Campaigns SET Name=$name, Description=$desc, UpdatedUtc=$u WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", rec.Id.ToString());
        cmd.Parameters.AddWithValue("$name", rec.Name);
        cmd.Parameters.AddWithValue("$desc", (object?)rec.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$u", rec.UpdatedUtc.ToString("o"));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    // -----------------
    // Characters
    // -----------------
    public async Task<IReadOnlyList<CharacterRecord>> ListCharactersAsync(CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Notes, ClassId, RaceId, Level, Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma, ArmorId, WeaponId, CreatedUtc, UpdatedUtc FROM Characters ORDER BY UpdatedUtc DESC;";
        var list = new List<CharacterRecord>();

        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            list.Add(ReadCharacter(r));

        return list;
    }

    public async Task<CharacterRecord?> GetCharacterAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Notes, ClassId, RaceId, Level, Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma, ArmorId, WeaponId, CreatedUtc, UpdatedUtc FROM Characters WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", id.ToString());

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        return ReadCharacter(r);
    }

    public async Task<CharacterRecord> CreateCharacterAsync(CharacterRecord character, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var rec = character with
        {
            Id = character.Id == Guid.Empty ? Guid.NewGuid() : character.Id,
            CreatedUtc = character.CreatedUtc == default ? now : character.CreatedUtc,
            UpdatedUtc = now
        };

        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Characters (Id, Name, Notes, ClassId, RaceId, Level, Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma, ArmorId, WeaponId, CreatedUtc, UpdatedUtc)
VALUES ($id,$name,$notes,$class,$race,$lvl,$str,$dex,$con,$int,$wis,$cha,$armor,$weapon,$c,$u);";
        cmd.Parameters.AddWithValue("$id", rec.Id.ToString());
        cmd.Parameters.AddWithValue("$name", rec.Name);
        cmd.Parameters.AddWithValue("$notes", (object?)rec.Notes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$class", (object?)rec.ClassId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$race", (object?)rec.RaceId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$lvl", rec.Level);
        cmd.Parameters.AddWithValue("$str", rec.Strength);
        cmd.Parameters.AddWithValue("$dex", rec.Dexterity);
        cmd.Parameters.AddWithValue("$con", rec.Constitution);
        cmd.Parameters.AddWithValue("$int", rec.Intelligence);
        cmd.Parameters.AddWithValue("$wis", rec.Wisdom);
        cmd.Parameters.AddWithValue("$cha", rec.Charisma);
        cmd.Parameters.AddWithValue("$armor", rec.ArmorId);
        cmd.Parameters.AddWithValue("$weapon", rec.WeaponId);
        cmd.Parameters.AddWithValue("$u", rec.UpdatedUtc.ToString("o"));
        await cmd.ExecuteNonQueryAsync(ct);

        return rec;
    }

    public async Task UpdateCharacterAsync(CharacterRecord character, CancellationToken ct = default)
    {
        var rec = character with { UpdatedUtc = DateTime.UtcNow };

        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE Characters
SET Name=$name, Notes=$notes, ClassId=$class, RaceId=$race, Level=$lvl,
    Strength=$str, Dexterity=$dex, Constitution=$con, Intelligence=$int, Wisdom=$wis, Charisma=$cha,
    ArmorId=$armor, WeaponId=$weapon,
    UpdatedUtc=$u
WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", rec.Id.ToString());
        cmd.Parameters.AddWithValue("$name", rec.Name);
        cmd.Parameters.AddWithValue("$notes", (object?)rec.Notes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$class", (object?)rec.ClassId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$race", (object?)rec.RaceId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$lvl", rec.Level);
        cmd.Parameters.AddWithValue("$str", rec.Strength);
        cmd.Parameters.AddWithValue("$dex", rec.Dexterity);
        cmd.Parameters.AddWithValue("$con", rec.Constitution);
        cmd.Parameters.AddWithValue("$int", rec.Intelligence);
        cmd.Parameters.AddWithValue("$wis", rec.Wisdom);
        cmd.Parameters.AddWithValue("$cha", rec.Charisma);
        cmd.Parameters.AddWithValue("$armor", rec.ArmorId);
        cmd.Parameters.AddWithValue("$weapon", rec.WeaponId);
        cmd.Parameters.AddWithValue("$u", rec.UpdatedUtc.ToString("o"));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    // -----------------
    // Encounters
    // -----------------
    public async Task<IReadOnlyList<EncounterRecord>> ListEncountersAsync(CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Notes, CreatedUtc, UpdatedUtc FROM Encounters ORDER BY UpdatedUtc DESC;";
        var list = new List<EncounterRecord>();

        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            list.Add(ReadEncounter(r));

        return list;
    }

    public async Task<EncounterRecord?> GetEncounterAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Notes, CreatedUtc, UpdatedUtc FROM Encounters WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", id.ToString());

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        return ReadEncounter(r);
    }

    public async Task<EncounterRecord> CreateEncounterAsync(EncounterRecord encounter, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var rec = encounter with
        {
            Id = encounter.Id == Guid.Empty ? Guid.NewGuid() : encounter.Id,
            CreatedUtc = encounter.CreatedUtc == default ? now : encounter.CreatedUtc,
            UpdatedUtc = now
        };

        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Encounters (Id, Name, Notes, CreatedUtc, UpdatedUtc)
VALUES ($id,$name,$notes,$c,$u);";
        cmd.Parameters.AddWithValue("$id", rec.Id.ToString());
        cmd.Parameters.AddWithValue("$name", rec.Name);
        cmd.Parameters.AddWithValue("$notes", (object?)rec.Notes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$u", rec.UpdatedUtc.ToString("o"));
        await cmd.ExecuteNonQueryAsync(ct);

        return rec;
    }

    public async Task UpdateEncounterAsync(EncounterRecord encounter, CancellationToken ct = default)
    {
        var rec = encounter with { UpdatedUtc = DateTime.UtcNow };

        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE Encounters SET Name=$name, Notes=$notes, UpdatedUtc=$u WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", rec.Id.ToString());
        cmd.Parameters.AddWithValue("$name", rec.Name);
        cmd.Parameters.AddWithValue("$notes", (object?)rec.Notes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$u", rec.UpdatedUtc.ToString("o"));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    // -----------------
    // Relations
    // -----------------
    public async Task SetCampaignCharactersAsync(Guid campaignId, IEnumerable<Guid> characterIds, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        await ExecAsync(conn, "DELETE FROM CampaignCharacters WHERE CampaignId=$c;", ct, ("$c", campaignId.ToString()));

        foreach (var id in characterIds.Distinct())
            await ExecAsync(conn, "INSERT INTO CampaignCharacters (CampaignId, CharacterId) VALUES ($c,$id);", ct, ("$c", campaignId.ToString()), ("$id", id.ToString()));

        await tx.CommitAsync(ct);
    }

    public async Task SetCampaignEncountersAsync(Guid campaignId, IEnumerable<Guid> encounterIds, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        await ExecAsync(conn, "DELETE FROM CampaignEncounters WHERE CampaignId=$c;", ct, ("$c", campaignId.ToString()));

        foreach (var id in encounterIds.Distinct())
            await ExecAsync(conn, "INSERT INTO CampaignEncounters (CampaignId, EncounterId) VALUES ($c,$id);", ct, ("$c", campaignId.ToString()), ("$id", id.ToString()));

        await tx.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<CharacterRecord>> GetCampaignCharactersAsync(Guid campaignId, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT ch.Id, ch.Name, ch.Notes, ch.ClassId, ch.RaceId, ch.Level, ch.CreatedUtc, ch.UpdatedUtc
FROM CampaignCharacters cc
JOIN Characters ch ON ch.Id = cc.CharacterId
WHERE cc.CampaignId = $c
ORDER BY ch.Name ASC;";
        cmd.Parameters.AddWithValue("$c", campaignId.ToString());

        var list = new List<CharacterRecord>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            list.Add(ReadCharacter(r));

        return list;
    }

    public async Task<IReadOnlyList<EncounterRecord>> GetCampaignEncountersAsync(Guid campaignId, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT e.Id, e.Name, e.Notes, e.CreatedUtc, e.UpdatedUtc
FROM CampaignEncounters ce
JOIN Encounters e ON e.Id = ce.EncounterId
WHERE ce.CampaignId = $c
ORDER BY e.Name ASC;";
        cmd.Parameters.AddWithValue("$c", campaignId.ToString());

        var list = new List<EncounterRecord>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            list.Add(ReadEncounter(r));

        return list;
    }

    public async Task SetEncounterMonstersAsync(Guid encounterId, IEnumerable<EncounterMonsterRecord> monsters, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        await ExecAsync(conn, "DELETE FROM EncounterMonsters WHERE EncounterId=$e;", ct, ("$e", encounterId.ToString()));

        foreach (var m in monsters)
        {
            if (string.IsNullOrWhiteSpace(m.MonsterId) || m.Count <= 0) continue;
            await ExecAsync(conn, "INSERT INTO EncounterMonsters (EncounterId, MonsterId, Count) VALUES ($e,$m,$c);", ct,
                ("$e", encounterId.ToString()), ("$m", m.MonsterId), ("$c", m.Count));
        }

        await tx.CommitAsync(ct);
    }

    public async Task SetEncounterCharactersAsync(Guid encounterId, IEnumerable<Guid> characterIds, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        await ExecAsync(conn, "DELETE FROM EncounterCharacters WHERE EncounterId=$e;", ct, ("$e", encounterId.ToString()));

        foreach (var id in characterIds.Distinct())
            await ExecAsync(conn, "INSERT INTO EncounterCharacters (EncounterId, CharacterId) VALUES ($e,$id);", ct,
                ("$e", encounterId.ToString()), ("$id", id.ToString()));

        await tx.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<EncounterMonsterRecord>> GetEncounterMonstersAsync(Guid encounterId, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT EncounterId, MonsterId, Count FROM EncounterMonsters WHERE EncounterId=$e ORDER BY MonsterId ASC;";
        cmd.Parameters.AddWithValue("$e", encounterId.ToString());

        var list = new List<EncounterMonsterRecord>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            list.Add(new EncounterMonsterRecord(Guid.Parse(r.GetString(0)), r.GetString(1), r.GetInt32(2)));

        return list;
    }

    public async Task<IReadOnlyList<CharacterRecord>> GetEncounterCharactersAsync(Guid encounterId, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT ch.Id, ch.Name, ch.Notes, ch.ClassId, ch.RaceId, ch.Level, ch.CreatedUtc, ch.UpdatedUtc
FROM EncounterCharacters ec
JOIN Characters ch ON ch.Id = ec.CharacterId
WHERE ec.EncounterId = $e
ORDER BY ch.Name ASC;";
        cmd.Parameters.AddWithValue("$e", encounterId.ToString());

        var list = new List<CharacterRecord>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            list.Add(ReadCharacter(r));

        return list;
    }

    // -----------------
    // Settings
    // -----------------
    public async Task<string?> GetSettingAsync(string key, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Value FROM Settings WHERE [Key]=$k;";
        cmd.Parameters.AddWithValue("$k", key);

        var scalar = await cmd.ExecuteScalarAsync(ct);
        return scalar?.ToString();
    }

    public async Task SetSettingAsync(string key, string value, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Settings ([Key],[Value]) VALUES ($k,$v)
ON CONFLICT([Key]) DO UPDATE SET [Value]=excluded.[Value];";
        cmd.Parameters.AddWithValue("$k", key);
        cmd.Parameters.AddWithValue("$v", value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    // -----------------
    // Helpers
    // -----------------
    

    // -----------------
    // Factions
    // -----------------
    async Task IFactionRepository.InitializeAsync(CancellationToken ct) => await InitializeAsync(ct);

    public async Task<IReadOnlyList<FactionRecord>> ListFactionsAsync(CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, InsigniaRef, CreatedUtc, UpdatedUtc FROM Factions ORDER BY Name ASC;";

        var list = new List<FactionRecord>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            list.Add(new FactionRecord(
                Guid.Parse(r.GetString(0)),
                r.GetString(1),
                r.IsDBNull(2) ? null : r.GetString(2),
                DateTime.Parse(r.GetString(3)).ToUniversalTime(),
                DateTime.Parse(r.GetString(4)).ToUniversalTime()));
        }
        return list;
    }

    public async Task<FactionRecord?> GetFactionAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, InsigniaRef, CreatedUtc, UpdatedUtc FROM Factions WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", id.ToString());

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;

        return new FactionRecord(
            Guid.Parse(r.GetString(0)),
            r.GetString(1),
            r.IsDBNull(2) ? null : r.GetString(2),
            DateTime.Parse(r.GetString(3)).ToUniversalTime(),
            DateTime.Parse(r.GetString(4)).ToUniversalTime());
    }

    public async Task<FactionRecord> UpsertFactionAsync(Guid? id, string name, string? insigniaRef, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));

        var fid = id ?? Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        // If exists: preserve CreatedUtc
        DateTime created = now;
        await using (var read = conn.CreateCommand())
        {
            read.CommandText = "SELECT CreatedUtc FROM Factions WHERE Id=$id;";
            read.Parameters.AddWithValue("$id", fid.ToString());
            var scalar = await read.ExecuteScalarAsync(ct);
            if (scalar is string s && DateTime.TryParse(s, out var parsed))
                created = parsed.ToUniversalTime();
        }

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
INSERT INTO Factions (Id, Name, InsigniaRef, CreatedUtc, UpdatedUtc)
VALUES ($id, $name, $ins, $created, $updated)
ON CONFLICT(Id) DO UPDATE SET
  Name=$name,
  InsigniaRef=$ins,
  UpdatedUtc=$updated;
";
            cmd.Parameters.AddWithValue("$id", fid.ToString());
            cmd.Parameters.AddWithValue("$name", name.Trim());
            cmd.Parameters.AddWithValue("$ins", (object?)insigniaRef ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$created", created.ToString("o"));
            cmd.Parameters.AddWithValue("$updated", now.ToString("o"));
            await cmd.ExecuteNonQueryAsync(ct);
        }

        return new FactionRecord(fid, name.Trim(), insigniaRef, created, now);
    }

    public async Task DeleteFactionAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Factions WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<int?> GetFactionRelationOverrideAsync(Guid sourceFactionId, Guid targetFactionId, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Score FROM FactionRelations WHERE SourceFactionId=$a AND TargetFactionId=$b;";
        cmd.Parameters.AddWithValue("$a", sourceFactionId.ToString());
        cmd.Parameters.AddWithValue("$b", targetFactionId.ToString());

        var scalar = await cmd.ExecuteScalarAsync(ct);
        if (scalar is long l) return (int)l;
        if (scalar is int i) return i;
        return null;
    }

    public async Task UpsertFactionRelationOverrideAsync(Guid sourceFactionId, Guid targetFactionId, int score, CancellationToken ct = default)
    {
        score = RelationshipBands.Clamp(score);

        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO FactionRelations (SourceFactionId, TargetFactionId, Score)
VALUES ($a, $b, $s)
ON CONFLICT(SourceFactionId, TargetFactionId) DO UPDATE SET Score=$s;
";
        cmd.Parameters.AddWithValue("$a", sourceFactionId.ToString());
        cmd.Parameters.AddWithValue("$b", targetFactionId.ToString());
        cmd.Parameters.AddWithValue("$s", score);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<CharacterFactionMembershipRecord>> GetCharacterFactionsAsync(Guid characterId, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT CharacterId, FactionId, IsPrimary, Rank, Title FROM CharacterFactions WHERE CharacterId=$id;";
        cmd.Parameters.AddWithValue("$id", characterId.ToString());

        var list = new List<CharacterFactionMembershipRecord>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            list.Add(new CharacterFactionMembershipRecord(
                Guid.Parse(r.GetString(0)),
                Guid.Parse(r.GetString(1)),
                r.GetInt32(2) != 0,
                r.GetInt32(3),
                r.IsDBNull(4) ? null : r.GetString(4)
            ));
        }
        return list;
    }

    public async Task SetCharacterFactionsAsync(Guid characterId, IEnumerable<CharacterFactionMembershipRecord> memberships, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        await using (var del = conn.CreateCommand())
        {
            del.Transaction = tx;
            del.CommandText = "DELETE FROM CharacterFactions WHERE CharacterId=$id;";
            del.Parameters.AddWithValue("$id", characterId.ToString());
            await del.ExecuteNonQueryAsync(ct);
        }

        foreach (var m in memberships)
        {
            await using var ins = conn.CreateCommand();
            ins.Transaction = tx;
            ins.CommandText = @"
INSERT INTO CharacterFactions (CharacterId, FactionId, IsPrimary, Rank, Title)
VALUES ($c, $f, $p, $r, $t);
";
            ins.Parameters.AddWithValue("$c", characterId.ToString());
            ins.Parameters.AddWithValue("$f", m.FactionId.ToString());
            ins.Parameters.AddWithValue("$p", m.IsPrimary ? 1 : 0);
            ins.Parameters.AddWithValue("$r", m.Rank);
            ins.Parameters.AddWithValue("$t", (object?)m.Title ?? DBNull.Value);
            await ins.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public async Task<int?> GetPersonalRelationshipAsync(Guid fromCharacterId, Guid toCharacterId, CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Score FROM CharacterPersonalRelations WHERE FromCharacterId=$a AND ToCharacterId=$b;";
        cmd.Parameters.AddWithValue("$a", fromCharacterId.ToString());
        cmd.Parameters.AddWithValue("$b", toCharacterId.ToString());

        var scalar = await cmd.ExecuteScalarAsync(ct);
        if (scalar is long l) return (int)l;
        if (scalar is int i) return i;
        return null;
    }

    public async Task UpsertPersonalRelationshipAsync(Guid fromCharacterId, Guid toCharacterId, int score, CancellationToken ct = default)
    {
        score = RelationshipBands.Clamp(score);

        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO CharacterPersonalRelations (FromCharacterId, ToCharacterId, Score)
VALUES ($a, $b, $s)
ON CONFLICT(FromCharacterId, ToCharacterId) DO UPDATE SET Score=$s;
";
        cmd.Parameters.AddWithValue("$a", fromCharacterId.ToString());
        cmd.Parameters.AddWithValue("$b", toCharacterId.ToString());
        cmd.Parameters.AddWithValue("$s", score);
        await cmd.ExecuteNonQueryAsync(ct);
    }

private SqliteConnection CreateConnection()
        => new($"Data Source={_dbPath}");

    private static CampaignRecord ReadCampaign(SqliteDataReader r)
        => new(Guid.Parse(r.GetString(0)), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2),
            DateTime.Parse(r.GetString(3), null, System.Globalization.DateTimeStyles.RoundtripKind),
            DateTime.Parse(r.GetString(4), null, System.Globalization.DateTimeStyles.RoundtripKind));

    private static CharacterRecord ReadCharacter(SqliteDataReader r)
        => new(
            Guid.Parse(r.GetString(0)),
            r.GetString(1),
            r.IsDBNull(2) ? null : r.GetString(2),
            r.IsDBNull(3) ? null : r.GetString(3),
            r.IsDBNull(4) ? null : r.GetString(4),
            r.GetInt32(5),
            r.GetInt32(6),
            r.GetInt32(7),
            r.GetInt32(8),
            r.GetInt32(9),
            r.GetInt32(10),
            r.GetInt32(11),
            r.GetString(12),
            r.GetString(13),
            DateTime.Parse(r.GetString(14), null, System.Globalization.DateTimeStyles.RoundtripKind),
            DateTime.Parse(r.GetString(15), null, System.Globalization.DateTimeStyles.RoundtripKind));

    private static EncounterRecord ReadEncounter(SqliteDataReader r)
        => new(Guid.Parse(r.GetString(0)), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2),
            DateTime.Parse(r.GetString(3), null, System.Globalization.DateTimeStyles.RoundtripKind),
            DateTime.Parse(r.GetString(4), null, System.Globalization.DateTimeStyles.RoundtripKind));

    private static async Task ExecAsync(SqliteConnection conn, string sql, CancellationToken ct, params (string name, object? value)[] p)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in p)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
