namespace SilverSpires.Tactics.Factions;

public sealed record FactionRecord(
    Guid Id,
    string Name,
    string? InsigniaRef,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);

public sealed record FactionRelationshipOverrideRecord(
    Guid SourceFactionId,
    Guid TargetFactionId,
    int Score);

public sealed record CharacterFactionMembershipRecord(
    Guid CharacterId,
    Guid FactionId,
    bool IsPrimary,
    int Rank,
    string? Title);

public sealed record CharacterPersonalRelationshipRecord(
    Guid FromCharacterId,
    Guid ToCharacterId,
    int Score);
