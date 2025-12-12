using SilverSpires.Tactics.Srd.Rules;

namespace SilverSpires.Tactics.Srd.Ingestion.Normalization;

public static class EnumParsers
{
    public static TEnum? TryParseEnum<TEnum>(string? value) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var e))
            return e;

        // Attempt common normalization: spaces -> nothing, hyphen -> nothing
        var normalized = value.Replace(" ", "").Replace("-", "");
        if (Enum.TryParse<TEnum>(normalized, ignoreCase: true, out e))
            return e;

        return null;
    }

    public static DamageType ParseDamageType(string? value, DamageType fallback = DamageType.Force)
        => TryParseEnum<DamageType>(value) ?? fallback;

    public static CreatureType ParseCreatureType(string? value)
        => TryParseEnum<CreatureType>(value) ?? CreatureType.Other;

    public static SizeCategory ParseSize(string? value)
        => TryParseEnum<SizeCategory>(value) ?? SizeCategory.Medium;
}
