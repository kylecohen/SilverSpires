using SilverSpires.Tactics.Factions;

namespace SilverSpires.Tactics.Combat;

public sealed class DefaultHostilityResolver : IHostilityResolver
{
    public bool AreHostile(Guid factionA, Guid factionB) => factionA != factionB;
}

/// <summary>
/// Encounter-scoped resolver: if global relations are neutral/unknown, combat still proceeds.
/// </summary>
public sealed class EncounterHostilityResolver : IHostilityResolver
{
    private readonly IHostilityResolver _baseResolver;

    public EncounterHostilityResolver(IHostilityResolver baseResolver)
    {
        _baseResolver = baseResolver;
    }

    public bool AreHostile(Guid factionA, Guid factionB)
    {
        if (factionA == factionB) return false;

        // If base says NOT hostile, treat that as "friendly/ally". Neutral defaults to hostile in encounters.
        // This is conservative for battle simulations; narrative systems can override.
        var baseHostile = _baseResolver.AreHostile(factionA, factionB);
        if (!baseHostile) return false;

        return true;
    }
}
