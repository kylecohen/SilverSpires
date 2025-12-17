using SilverSpires.Tactics.Srd.Ingestion.Abstractions;
using SilverSpires.Tactics.Srd.Persistence.Registry;
using SilverSpires.Tactics.Srd.Persistence.Storage.Json;
using SilverSpires.Tactics.Srd.Rules;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SilverSpires.Tactics.Srd.Ingestion.Mapping;

public sealed class GenericMappingEngine : IMappingEngine
{
    private readonly JsonSerializerOptions _json;

    private static readonly Dictionary<string, string[]> Synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Strength"] = new[] { "strength", "str" },
        ["Dexterity"] = new[] { "dexterity", "dex" },
        ["Constitution"] = new[] { "constitution", "con" },
        ["Intelligence"] = new[] { "intelligence", "int" },
        ["Wisdom"] = new[] { "wisdom", "wis" },
        ["Charisma"] = new[] { "charisma", "cha" },

        ["Id"] = new[] { "id", "slug", "key", "identifier" },
        ["ArmorClass"] = new[] { "armor_class", "ac", "ArmorClass" },
        ["HitPointsAverage"] = new[] { "hit_points", "hp", "HitPoints" },
        ["HitDice"] = new[] { "hit_dice", "HitDice" },
        ["ChallengeRating"] = new[] { "challenge_rating", "cr", "ChallengeRating" },
    };

    public GenericMappingEngine()
    {
        _json = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        _json.Converters.Add(new JsonStringEnumConverter()); // optional now
        _json.Converters.Add(new ChallengeRatingJsonConverter());

        // Add safe converters for enums you know are problematic:
        _json.Converters.Add(new SafeEnumJsonConverter<AbilityScoreType>(
            new Dictionary<string, AbilityScoreType>(StringComparer.OrdinalIgnoreCase)
            {
                ["str"] = AbilityScoreType.Strength,
                ["dex"] = AbilityScoreType.Dexterity,
                ["con"] = AbilityScoreType.Constitution,
                ["int"] = AbilityScoreType.Intelligence,
                ["wis"] = AbilityScoreType.Wisdom,
                ["cha"] = AbilityScoreType.Charisma,
            }));
    }

    public MappingResult<T> Map<T>(JsonElement sourceObj, MappingProfile profile, SrdSourceMetadata meta)
    {
        var result = new MappingResult<T>();

        try
        {
            var map = SimpleFieldMap.FromJson(profile.RulesJson);
            var targetType = typeof(T);

            var instance = Activator.CreateInstance(targetType);
            if (instance is null)
            {
                result.Errors.Add($"Could not create instance of {targetType.FullName}");
                return result;
            }

            foreach (var prop in targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanWrite) continue;

                var targetName = prop.Name;

                map.Fields.TryGetValue(targetName, out var ruleValue);

                if (string.Equals(ruleValue, SimpleFieldMap.NotAvailableToken, StringComparison.OrdinalIgnoreCase))
                    continue; // explicitly not mapped

                // auto-match when null/empty
                string? sourceField = null;
                if (string.IsNullOrWhiteSpace(ruleValue))
                {
                    sourceField = FindBestSourceField(sourceObj, targetName);
                }
                else
                {
                    sourceField = ruleValue;
                }

                if (string.IsNullOrWhiteSpace(sourceField))
                    continue;

                if (!TryGetJsonValue(sourceObj, sourceField!, out var jsonValue))
                    continue;

                var coerced = Coerce(jsonValue, prop.PropertyType);
                if (coerced is null) continue;

                prop.SetValue(instance, coerced);
            }

            result.Entity = (T)instance;
            result.IsSuccess = true;
            return result;
        }
        catch (Exception ex)
        {
            result.Errors.Add(ex.ToString());
            return result;
        }
    }

    private static string? FindBestSourceField(JsonElement obj, string targetProp)
    {
        if (obj.ValueKind != JsonValueKind.Object) return null;

        // exact match (case-insensitive)
        foreach (var p in obj.EnumerateObject())
            if (string.Equals(p.Name, targetProp, StringComparison.OrdinalIgnoreCase))
                return p.Name;

        // synonyms match
        if (Synonyms.TryGetValue(targetProp, out var syns))
        {
            foreach (var s in syns)
            {
                foreach (var p in obj.EnumerateObject())
                    if (string.Equals(p.Name, s, StringComparison.OrdinalIgnoreCase))
                        return p.Name;
            }
        }

        // snake_case match
        var snake = ToSnakeCase(targetProp);
        foreach (var p in obj.EnumerateObject())
            if (string.Equals(p.Name, snake, StringComparison.OrdinalIgnoreCase))
                return p.Name;

        return null;
    }

    private static string ToSnakeCase(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        var chars = new List<char>(s.Length + 8);
        for (int i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (char.IsUpper(c) && i > 0) chars.Add('_');
            chars.Add(char.ToLowerInvariant(c));
        }
        return new string(chars.ToArray());
    }

    private static bool TryGetJsonValue(JsonElement obj, string fieldOrPath, out JsonElement value)
    {
        value = default;

        if (obj.ValueKind != JsonValueKind.Object) return false;

        // support dot paths: "a.b.c"
        var parts = fieldOrPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        JsonElement current = obj;

        foreach (var part in parts)
        {
            if (current.ValueKind != JsonValueKind.Object) return false;
            if (!current.TryGetProperty(part, out var next)) return false;
            current = next;
        }

        value = current;
        return true;
    }

    private object? Coerce(JsonElement value, Type targetType)
    {
        try
        {
            // nullables
            var nt = Nullable.GetUnderlyingType(targetType);
            if (nt != null) targetType = nt;

            if (targetType == typeof(string))
                return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();

            if (targetType == typeof(int))
            {
                if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var i)) return i;
                if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var si)) return si;
            }

            if (targetType == typeof(double))
            {
                if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var d)) return d;
                if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), out var sd)) return sd;
            }

            if (targetType == typeof(bool))
            {
                if (value.ValueKind == JsonValueKind.True) return true;
                if (value.ValueKind == JsonValueKind.False) return false;
                if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var sb)) return sb;
            }

            if (targetType.IsEnum && value.ValueKind == JsonValueKind.String)
            {
                var s = value.GetString();
                if (!string.IsNullOrWhiteSpace(s) && Enum.TryParse(targetType, s, true, out var parsed))
                    return parsed;
            }

            if (targetType == typeof(ChallengeRating))
            {
                Console.WriteLine($"CR raw={value.GetRawText()} kind={value.ValueKind}");
            }

            // lists/complex types: try JsonSerializer
            var raw = value.GetRawText();
            return JsonSerializer.Deserialize(raw, targetType, _json);
        }
        catch (Exception ex)
        {
            Console.Write(ex);
            return null;
        }
    }
}
