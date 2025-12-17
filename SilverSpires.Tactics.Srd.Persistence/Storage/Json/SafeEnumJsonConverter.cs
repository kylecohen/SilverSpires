using System.Text.Json;
using System.Text.Json.Serialization;

namespace SilverSpires.Tactics.Srd.Persistence.Storage.Json;

public sealed class SafeEnumJsonConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
{
    private readonly Dictionary<string, TEnum> _aliases;

    public SafeEnumJsonConverter(Dictionary<string, TEnum>? aliases = null)
    {
        _aliases = aliases ?? new Dictionary<string, TEnum>(StringComparer.OrdinalIgnoreCase);
    }

    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (string.IsNullOrWhiteSpace(s))
                return default;

            if (_aliases.TryGetValue(s, out var aliased))
                return aliased;

            if (Enum.TryParse<TEnum>(s, ignoreCase: true, out var parsed))
                return parsed;

            // try common normalized forms
            var normalized = s.Replace(" ", "").Replace("_", "");
            if (Enum.TryParse<TEnum>(normalized, ignoreCase: true, out parsed))
                return parsed;

            return default; // swallow unknown enum values
        }

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var i))
        {
            if (Enum.IsDefined(typeof(TEnum), i))
                return (TEnum)Enum.ToObject(typeof(TEnum), i);

            return default;
        }

        if (reader.TokenType == JsonTokenType.Null)
            return default;

        throw new JsonException($"Unexpected token {reader.TokenType} for enum {typeof(TEnum).Name}");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
