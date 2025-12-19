namespace SilverSpires.Tactics.Game;

public sealed record CampaignRecord(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedUtc,
    DateTime UpdatedUtc
);

public sealed record CharacterRecord(
    Guid Id,
    string Name,
    string? Notes,
    // Optional SRD selections
    string? ClassId,
    string? RaceId,
    int Level,
    // Core stats
    int Strength,
    int Dexterity,
    int Constitution,
    int Intelligence,
    int Wisdom,
    int Charisma,
    // Equipment (SRD ids)
    string ArmorId,
    string WeaponId,
    DateTime CreatedUtc,
    DateTime UpdatedUtc
);

public sealed record EncounterRecord(
    Guid Id,
    string Name,
    string? Notes,
    DateTime CreatedUtc,
    DateTime UpdatedUtc
);

public sealed record CampaignSelection(
    Guid CampaignId,
    Guid EntityId
);

public sealed record EncounterMonsterRecord(
    Guid EncounterId,
    string MonsterId,
    int Count
);

public sealed record EncounterCharacterRecord(
    Guid EncounterId,
    Guid CharacterId
);

public sealed record AppSetting(
    string Key,
    string Value
);
