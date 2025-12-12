using SilverSpires.Tactics.Srd.Rules;

namespace SilverSpires.Tactics.Srd.Ingestion.Normalization;

public static class AbilityScoreParser
{
    public static AbilityScoreType? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim().ToLowerInvariant();

        return value switch
        {
            "str" or "strength" => AbilityScoreType.Strength,
            "dex" or "dexterity" => AbilityScoreType.Dexterity,
            "con" or "constitution" => AbilityScoreType.Constitution,
            "int" or "intelligence" => AbilityScoreType.Intelligence,
            "wis" or "wisdom" => AbilityScoreType.Wisdom,
            "cha" or "charisma" => AbilityScoreType.Charisma,
            _ => null
        };
    }
}
