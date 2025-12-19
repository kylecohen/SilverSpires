namespace SilverSpires.Tactics.Factions;

public interface IFactionRepository
{
    Task InitializeAsync(CancellationToken ct = default);

    Task<IReadOnlyList<FactionRecord>> ListFactionsAsync(CancellationToken ct = default);
    Task<FactionRecord?> GetFactionAsync(Guid id, CancellationToken ct = default);
    Task<FactionRecord> UpsertFactionAsync(Guid? id, string name, string? insigniaRef, CancellationToken ct = default);
    Task DeleteFactionAsync(Guid id, CancellationToken ct = default);

    Task<int?> GetFactionRelationOverrideAsync(Guid sourceFactionId, Guid targetFactionId, CancellationToken ct = default);
    Task UpsertFactionRelationOverrideAsync(Guid sourceFactionId, Guid targetFactionId, int score, CancellationToken ct = default);

    Task<IReadOnlyList<CharacterFactionMembershipRecord>> GetCharacterFactionsAsync(Guid characterId, CancellationToken ct = default);
    Task SetCharacterFactionsAsync(Guid characterId, IEnumerable<CharacterFactionMembershipRecord> memberships, CancellationToken ct = default);

    Task<int?> GetPersonalRelationshipAsync(Guid fromCharacterId, Guid toCharacterId, CancellationToken ct = default);
    Task UpsertPersonalRelationshipAsync(Guid fromCharacterId, Guid toCharacterId, int score, CancellationToken ct = default);
}

public interface IRelationshipService
{
    Task<int> GetFactionRelationScoreAsync(Guid sourceFactionId, Guid targetFactionId, CancellationToken ct = default);
    Task<int> ComputeFactionInfluenceAsync(Guid fromCharacterId, Guid toCharacterId, CancellationToken ct = default);
    Task<int> ComputeFinalPersonalRelationshipAsync(Guid fromCharacterId, Guid toCharacterId, CancellationToken ct = default);
    RelationshipBand GetBand(int score);
}

public interface IHostilityResolver
{
    bool AreHostile(Guid factionA, Guid factionB);
}
