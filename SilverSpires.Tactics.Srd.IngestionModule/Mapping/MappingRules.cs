namespace SilverSpires.Tactics.Srd.Ingestion.Mapping;

/// <summary>
/// Mapping profile rules JSON schema.
/// This intentionally stays simple and is easy to hand-author.
/// </summary>
public sealed class MappingRules
{
    /// <summary>
    /// When true, missing fields are warnings instead of errors.
    /// </summary>
    public bool BestEffort { get; set; } = true;

    public List<FieldRule> Fields { get; set; } = new();
}

public sealed class FieldRule
{
    /// <summary>
    /// Target property name on the canonical SRD model (e.g., "Name", "Strength").
    /// Supports nested targets using dot-path (e.g., "ChallengeRating.Numerator").
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// One or more source paths (fallbacks). Simple dot-paths for JSON objects.
    /// Examples: "name", "slug", "stats.str".
    /// </summary>
    public List<string> Source { get; set; } = new();

    /// <summary>
    /// Optional constant override (string JSON).
    /// Example: { "Target": "Tags", "ConstantJson": "[\"SRD\"]" }
    /// </summary>
    public string? ConstantJson { get; set; }

    /// <summary>
    /// Optional transform name (built-ins): "trim", "lower", "upper",
    /// "parse_size", "parse_creature_type", "parse_damage_type", "parse_cr".
    /// </summary>
    public string? Transform { get; set; }

    /// <summary>
    /// If true, missing value becomes an error (even if BestEffort is true).
    /// </summary>
    public bool Required { get; set; } = false;
}
