namespace SilverSpires.Tactics.Game;

public interface IGameRepository
{
    Task InitializeAsync(CancellationToken ct = default);

    // Campaigns
    Task<IReadOnlyList<CampaignRecord>> ListCampaignsAsync(CancellationToken ct = default);
    Task<CampaignRecord?> GetCampaignAsync(Guid id, CancellationToken ct = default);
    Task<CampaignRecord> CreateCampaignAsync(string name, string? description, CancellationToken ct = default);
    Task UpdateCampaignAsync(CampaignRecord campaign, CancellationToken ct = default);

    // Characters
    Task<IReadOnlyList<CharacterRecord>> ListCharactersAsync(CancellationToken ct = default);
    Task<CharacterRecord?> GetCharacterAsync(Guid id, CancellationToken ct = default);
    Task<CharacterRecord> CreateCharacterAsync(CharacterRecord character, CancellationToken ct = default);
    Task UpdateCharacterAsync(CharacterRecord character, CancellationToken ct = default);

    // Encounters
    Task<IReadOnlyList<EncounterRecord>> ListEncountersAsync(CancellationToken ct = default);
    Task<EncounterRecord?> GetEncounterAsync(Guid id, CancellationToken ct = default);
    Task<EncounterRecord> CreateEncounterAsync(EncounterRecord encounter, CancellationToken ct = default);
    Task UpdateEncounterAsync(EncounterRecord encounter, CancellationToken ct = default);

    // Relations
    Task SetCampaignCharactersAsync(Guid campaignId, IEnumerable<Guid> characterIds, CancellationToken ct = default);
    Task SetCampaignEncountersAsync(Guid campaignId, IEnumerable<Guid> encounterIds, CancellationToken ct = default);

    Task<IReadOnlyList<CharacterRecord>> GetCampaignCharactersAsync(Guid campaignId, CancellationToken ct = default);
    Task<IReadOnlyList<EncounterRecord>> GetCampaignEncountersAsync(Guid campaignId, CancellationToken ct = default);

    Task SetEncounterMonstersAsync(Guid encounterId, IEnumerable<EncounterMonsterRecord> monsters, CancellationToken ct = default);
    Task SetEncounterCharactersAsync(Guid encounterId, IEnumerable<Guid> characterIds, CancellationToken ct = default);

    Task<IReadOnlyList<EncounterMonsterRecord>> GetEncounterMonstersAsync(Guid encounterId, CancellationToken ct = default);
    Task<IReadOnlyList<CharacterRecord>> GetEncounterCharactersAsync(Guid encounterId, CancellationToken ct = default);

    // Settings (for last sync marker, etc.)
    Task<string?> GetSettingAsync(string key, CancellationToken ct = default);
    Task SetSettingAsync(string key, string value, CancellationToken ct = default);
}
