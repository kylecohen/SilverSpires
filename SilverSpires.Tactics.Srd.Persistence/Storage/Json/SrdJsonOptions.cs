using System.Text.Json;
using System.Text.Json.Serialization;
using SilverSpires.Tactics.Srd.Persistence.Storage.Json;
using SilverSpires.Tactics.Srd.Rules;

namespace SilverSpires.Tactics.Srd.Persistence.Storage.Json;

/// <summary>
/// Central place for SRD JsonSerializerOptions so CLI, API, and repositories deserialize consistently.
/// </summary>
public static class SrdJsonOptions
{
    public static JsonSerializerOptions CreateDefault()
    {
        var json = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Basic enums
        json.Converters.Add(new JsonStringEnumConverter());

        // SRD custom structs
        json.Converters.Add(new ChallengeRatingJsonConverter());

        // Add safe converters for enums you know are problematic:
        json.Converters.Add(new SafeEnumJsonConverter<AbilityScoreType>(
            new Dictionary<string, AbilityScoreType>(StringComparer.OrdinalIgnoreCase)
            {
                ["str"] = AbilityScoreType.Strength,
                ["dex"] = AbilityScoreType.Dexterity,
                ["con"] = AbilityScoreType.Constitution,
                ["int"] = AbilityScoreType.Intelligence,
                ["wis"] = AbilityScoreType.Wisdom,
                ["cha"] = AbilityScoreType.Charisma,
            }));

        return json;
    }
}
