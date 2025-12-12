using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using SilverSpires.Tactics.Srd.Ingestion.Abstractions;
using SilverSpires.Tactics.Srd.Ingestion.Normalization;
using SilverSpires.Tactics.Srd.Ingestion.Mapping;
using SilverSpires.Tactics.Srd.Rules;

namespace SilverSpires.Tactics.Srd.Ingestion.Mapping;

public sealed class GenericMappingEngine : IMappingEngine
{
    private readonly JsonSerializerOptions _json;

    public GenericMappingEngine()
    {
        _json = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        _json.Converters.Add(new JsonStringEnumConverter());
    }

    public MappingResult<TTarget> Map<TTarget>(
        JsonElement sourceObject,
        MappingProfile profile,
        SrdSourceMetadata metadata)
    {
        var warnings = new List<string>();
        var errors = new List<string>();

        MappingRules rules;
        try
        {
            rules = JsonSerializer.Deserialize<MappingRules>(profile.RulesJson, _json) ?? new MappingRules();
        }
        catch (Exception ex)
        {
            return MappingResult<TTarget>.Failure($"Invalid RulesJson for profile '{profile.Id}': {ex.Message}");
        }

        var target = Activator.CreateInstance<TTarget>();

        foreach (var field in rules.Fields)
        {
            try
            {
                JsonElement? value = null;

                if (!string.IsNullOrWhiteSpace(field.ConstantJson))
                {
                    value = JsonSerializer.Deserialize<JsonElement>(field.ConstantJson!, _json);
                }
                else
                {
                    foreach (var path in field.Source)
                    {
                        if (TryGetByPath(sourceObject, path, out var found))
                        {
                            value = found;
                            break;
                        }
                    }
                }

                if (value is null || value.Value.ValueKind == JsonValueKind.Undefined || value.Value.ValueKind == JsonValueKind.Null)
                {
                    var msg = $"Missing '{string.Join(" | ", field.Source)}' for target '{field.Target}' (profile {profile.Id})";
                    if (field.Required) errors.Add(msg);
                    else if (rules.BestEffort) warnings.Add(msg);
                    else errors.Add(msg);
                    continue;
                }

                object? coerced = Coerce(value.Value, GetTargetPropertyType<TTarget>(field.Target), field.Transform, warnings);

                if (!TrySetTarget(target!, field.Target, coerced))
                {
                    var msg = $"Failed to set target '{field.Target}' on {typeof(TTarget).Name}";
                    if (field.Required) errors.Add(msg); else warnings.Add(msg);
                }
            }
            catch (Exception ex)
            {
                var msg = $"Exception mapping target '{field.Target}': {ex.Message}";
                if (field.Required) errors.Add(msg); else warnings.Add(msg);
            }
        }

        if (errors.Count > 0)
            return new MappingResult<TTarget> { Entity = default, Errors = errors, Warnings = warnings };

        return new MappingResult<TTarget> { Entity = target, Errors = errors, Warnings = warnings };
    }

    private static bool TryGetByPath(JsonElement root, string path, out JsonElement value)
    {
        value = default;

        if (string.IsNullOrWhiteSpace(path))
            return false;

        var current = root;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current.ValueKind != JsonValueKind.Object)
                return false;

            if (!current.TryGetProperty(segment, out var next))
                return false;

            current = next;
        }

        value = current;
        return true;
    }

    private static Type GetTargetPropertyType<TTarget>(string targetPath)
    {
        var type = typeof(TTarget);
        foreach (var seg in targetPath.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            var prop = type.GetProperty(seg, BindingFlags.Public | BindingFlags.Instance);
            if (prop is null) return typeof(object);
            type = prop.PropertyType;
        }
        return type;
    }

    private object? Coerce(JsonElement value, Type targetType, string? transform, List<string> warnings)
    {
        // Nullable unwrap
        var underlying = Nullable.GetUnderlyingType(targetType);
        if (underlying != null) targetType = underlying;

        // Transform string-like values first
        if (!string.IsNullOrWhiteSpace(transform))
        {
            var t = transform.Trim().ToLowerInvariant();

            // string transforms
            if (value.ValueKind == JsonValueKind.String)
            {
                var s = value.GetString() ?? "";
                s = t switch
                {
                    "trim" => s.Trim(),
                    "lower" => s.ToLowerInvariant(),
                    "upper" => s.ToUpperInvariant(),
                    _ => s
                };
                value = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(s));
            }

            if (t == "parse_size")
            {
                var s = value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
                var parsed = EnumParsers.ParseSize(s);
                return parsed;
            }
            if (t == "parse_creature_type")
            {
                var s = value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
                var parsed = EnumParsers.ParseCreatureType(s);
                return parsed;
            }
            if (t == "parse_damage_type")
            {
                var s = value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
                var parsed = EnumParsers.ParseDamageType(s);
                return parsed;
            }
            if (t == "parse_cr")
            {
                var s = value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
                var parsed = ChallengeRatingParser.Parse(s);
                if (parsed == null) warnings.Add($"Could not parse CR '{s}'");
                return parsed ?? new ChallengeRating(0, 1);
            }
        }

        // Direct JSON deserialization into the target type
        try
        {
            var raw = value.GetRawText();
            return JsonSerializer.Deserialize(raw, targetType, _json);
        }
        catch
        {
            // fallback conversions
            if (targetType == typeof(string)) return value.ToString();
            if (targetType == typeof(int) && value.TryGetInt32(out var i)) return i;
            if (targetType == typeof(double) && value.TryGetDouble(out var d)) return d;
            if (targetType.IsEnum && value.ValueKind == JsonValueKind.String)
            {
                var s = value.GetString();
                if (!string.IsNullOrWhiteSpace(s) && Enum.TryParse(targetType, s, ignoreCase: true, out var parsed))
                    return parsed;
            }
            return null;
        }
    }

    private static bool TrySetTarget<TTarget>(TTarget target, string targetPath, object? value)
    {
        if (target == null) return false;

        var segments = targetPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
        object current = target!;
        Type currentType = typeof(TTarget);

        for (int i = 0; i < segments.Length; i++)
        {
            var seg = segments[i];
            var prop = currentType.GetProperty(seg, BindingFlags.Public | BindingFlags.Instance);
            if (prop is null) return false;

            if (i == segments.Length - 1)
            {
                if (!prop.CanWrite) return false;

                // if value is null and target is non-nullable value type, skip
                if (value == null && prop.PropertyType.IsValueType && Nullable.GetUnderlyingType(prop.PropertyType) == null)
                    return false;

                prop.SetValue(current, value);
                return true;
            }
            else
            {
                var next = prop.GetValue(current);
                if (next == null)
                {
                    next = Activator.CreateInstance(prop.PropertyType);
                    prop.SetValue(current, next);
                }
                current = next!;
                currentType = prop.PropertyType;
            }
        }

        return false;
    }
}
