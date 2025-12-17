using SilverSpires.Tactics.Srd.Characters;
using SilverSpires.Tactics.Srd.Items;
using SilverSpires.Tactics.Srd.Monsters;
using SilverSpires.Tactics.Srd.Persistence.Registry;
using SilverSpires.Tactics.Srd.Persistence.Storage.Sqlite;
using SilverSpires.Tactics.Srd.Persistence.Storage.SqlServer;
using SilverSpires.Tactics.Srd.Rules;
using SilverSpires.Tactics.Srd.Spells;

namespace SilverSpires.Tactics.Srd.Persistence.Storage;

public interface ISrdRepository
{
    Task InitializeAsync(CancellationToken ct = default);

    // Registry
    Task UpsertSourceAsync(SourceDefinition source, CancellationToken ct = default);
    Task UpsertMappingProfileAsync(MappingProfile profile, CancellationToken ct = default);
    Task UpsertFeedAsync(SourceEntityFeed feed, CancellationToken ct = default);

    Task<IReadOnlyList<SourceDefinition>> GetSourcesAsync(CancellationToken ct = default);
    Task<SourceDefinition?> GetSourceAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<SourceEntityFeed>> GetFeedsBySourceAsync(string sourceId, bool enabledOnly, CancellationToken ct = default);
    Task<MappingProfile?> GetMappingProfileAsync(string id, CancellationToken ct = default);

    // Canonical SRD entities (Upsert)
    Task UpsertClassAsync(SrdClass entity, CancellationToken ct = default);
    Task UpsertRaceAsync(SrdRace entity, CancellationToken ct = default);
    Task UpsertBackgroundAsync(SrdBackground entity, CancellationToken ct = default);
    Task UpsertFeatAsync(SrdFeat entity, CancellationToken ct = default);
    Task UpsertSkillAsync(SrdSkill entity, CancellationToken ct = default);
    Task UpsertLanguageAsync(SrdLanguage entity, CancellationToken ct = default);
    Task UpsertSpellAsync(SrdSpell entity, CancellationToken ct = default);
    Task UpsertMonsterAsync(SrdMonster entity, CancellationToken ct = default);
    Task UpsertMagicItemAsync(SrdMagicItem entity, CancellationToken ct = default);
    Task UpsertEquipmentAsync(SrdEquipment entity, CancellationToken ct = default);
    Task UpsertWeaponAsync(SrdWeapon entity, CancellationToken ct = default);
    Task UpsertArmorAsync(SrdArmor entity, CancellationToken ct = default);
    Task UpsertEffectAsync(GameEffect entity, CancellationToken ct = default);

    // Canonical SRD entities (Read)
    Task<IReadOnlyList<SrdClass>> GetAllClassesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SrdRace>> GetAllRacesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SrdBackground>> GetAllBackgroundsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SrdFeat>> GetAllFeatsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SrdSkill>> GetAllSkillsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SrdLanguage>> GetAllLanguagesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SrdSpell>> GetAllSpellsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SrdMonster>> GetAllMonstersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SrdMagicItem>> GetAllMagicItemsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SrdEquipment>> GetAllEquipmentAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SrdWeapon>> GetAllWeaponsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SrdArmor>> GetAllArmorAsync(CancellationToken ct = default);
    Task<IReadOnlyList<GameEffect>> GetAllEffectsAsync(CancellationToken ct = default);

    public static ISrdRepository CreateRepository()
    {
        var sqlCs = Environment.GetEnvironmentVariable("SRD_SQL_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(sqlCs))
            return new SqlServerSrdRepository(sqlCs);

        var sqlitePath = Environment.GetEnvironmentVariable("SRD_SQLITE_PATH");
        if (string.IsNullOrWhiteSpace(sqlitePath))
            sqlitePath = Path.Combine(AppContext.BaseDirectory, "srd_cache.sqlite");

        return new SqliteSrdRepository(sqlitePath);
    }
}
