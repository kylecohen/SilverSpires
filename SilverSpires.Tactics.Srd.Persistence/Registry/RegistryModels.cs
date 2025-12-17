using System.Text.Json;

namespace SilverSpires.Tactics.Srd.Persistence.Registry;

public enum SrdSourceKind
{
    FileJson = 0,
    HttpJson = 1
}

public enum SrdEntityType
{
    Class,
    Race,
    Background,
    Feat,
    Skill,
    Language,
    Spell,
    Monster,
    MagicItem,
    Equipment,
    Weapon,
    Armor,
    Effect
}

public sealed class SourceDefinition
{
    public string Id { get; set; } = string.Empty;     // e.g. "open5e"
    public string Name { get; set; } = string.Empty;   // e.g. "Open5e"
    public SrdSourceKind Kind { get; set; }
    public string ConnectionJson { get; set; } = "{}"; // kind-specific settings (basePath/baseUrl/headers)
    public bool IsEnabled { get; set; } = true;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public T? GetConnection<T>() where T : class
        => JsonSerializer.Deserialize<T>(ConnectionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
}

public sealed class MappingProfile
{
    public string Id { get; set; } = string.Empty;     // e.g. "open5e_monster_default"
    public string Name { get; set; } = string.Empty;
    public SrdEntityType EntityType { get; set; }
    public string RulesJson { get; set; } = "{}";      // mapping rules blob (SimpleFieldMap)
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class SourceEntityFeed
{
    public string Id { get; set; } = string.Empty;          // e.g. "open5e_monsters"
    public string SourceId { get; set; } = string.Empty;    // FK to SourceDefinition.Id
    public SrdEntityType EntityType { get; set; }
    public string FeedJson { get; set; } = "{}";            // kind-specific fetch config (url/path/root)
    public string MappingProfileId { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public T? GetFeed<T>() where T : class
        => JsonSerializer.Deserialize<T>(FeedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
}
