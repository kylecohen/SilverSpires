namespace SilverSpires.Tactics.Srd.Ingestion.Abstractions;

/// <summary>
/// Source configuration stored in DB. ConnectionJson stores per-kind config.
/// Examples:
/// - FileJson: { "basePath": "C:\\data" }
/// - HttpJson: { "baseUrl": "https://example.com/api/" , "headers": { "Authorization": "Bearer ..." } }
/// </summary>
public sealed class SourceDefinition
{
    public string Id { get; set; } = string.Empty;          // stable key, e.g. "official_srd"
    public string Name { get; set; } = string.Empty;
    public SrdSourceKind Kind { get; set; }
    public string ConnectionJson { get; set; } = "{}";
    public bool IsEnabled { get; set; } = true;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Mapping profile stored in DB. RulesJson is a JSON document describing field mappings.
/// </summary>
public sealed class MappingProfile
{
    public string Id { get; set; } = string.Empty;          // stable key
    public string Name { get; set; } = string.Empty;
    public SrdEntityType EntityType { get; set; }
    public string TargetClrType { get; set; } = string.Empty; // optional informational field
    public string RulesJson { get; set; } = "{}";
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Connects a SourceDefinition to a specific entity feed (monsters/spells/etc),
/// referencing a MappingProfile.
/// </summary>
public sealed class SourceEntityFeed
{
    public string Id { get; set; } = string.Empty;          // stable key
    public string SourceId { get; set; } = string.Empty;
    public SrdEntityType EntityType { get; set; }

    /// <summary>
    /// FeedJson contains:
    /// - FileJson: { "path": "monsters.json", "root": "$" or "$.results" }
    /// - HttpJson: { "url": "monsters/", "root": "$.results", "pagination": {...} }
    /// </summary>
    public string FeedJson { get; set; } = "{}";

    public string MappingProfileId { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
