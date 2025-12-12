using System.Text.Json.Serialization;

namespace SilverSpires.Tactics.Srd.Ingestion.Sources.Open5e;

/// <summary>
/// Open5e uses snake_case. These DTOs are deliberately source-shaped.
/// </summary>
public sealed class Open5eMonsterDto
{
    [JsonPropertyName("slug")] public string Slug { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";

    [JsonPropertyName("size")] public string Size { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("alignment")] public string Alignment { get; set; } = "";

    [JsonPropertyName("armor_class")] public int ArmorClass { get; set; }
    [JsonPropertyName("hit_points")] public int HitPoints { get; set; }
    [JsonPropertyName("hit_dice")] public string HitDice { get; set; } = "";

    [JsonPropertyName("speed")] public Dictionary<string, int>? Speed { get; set; }

    [JsonPropertyName("strength")] public int Strength { get; set; }
    [JsonPropertyName("dexterity")] public int Dexterity { get; set; }
    [JsonPropertyName("constitution")] public int Constitution { get; set; }
    [JsonPropertyName("intelligence")] public int Intelligence { get; set; }
    [JsonPropertyName("wisdom")] public int Wisdom { get; set; }
    [JsonPropertyName("charisma")] public int Charisma { get; set; }

    [JsonPropertyName("skills")] public Dictionary<string, int>? Skills { get; set; }
    [JsonPropertyName("senses")] public string? Senses { get; set; }
    [JsonPropertyName("languages")] public string? Languages { get; set; }

    [JsonPropertyName("challenge_rating")] public string? ChallengeRating { get; set; }

    [JsonPropertyName("actions")] public string? Actions { get; set; }
    [JsonPropertyName("special_abilities")] public string? Traits { get; set; }
    [JsonPropertyName("reactions")] public string? Reactions { get; set; }
    [JsonPropertyName("legendary_actions")] public string? LegendaryActions { get; set; }
}

/// <summary>Open5e list response (paginated).</summary>
public sealed class Open5eListResponse<T>
{
    [JsonPropertyName("count")] public int Count { get; set; }
    [JsonPropertyName("next")] public string? Next { get; set; }
    [JsonPropertyName("previous")] public string? Previous { get; set; }
    [JsonPropertyName("results")] public List<T> Results { get; set; } = new();
}
