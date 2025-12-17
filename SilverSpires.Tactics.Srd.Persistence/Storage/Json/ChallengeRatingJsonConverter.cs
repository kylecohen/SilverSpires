using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using SilverSpires.Tactics.Srd.Rules;

namespace SilverSpires.Tactics.Srd.Persistence.Storage.Json;

public sealed class ChallengeRatingJsonConverter : JsonConverter<ChallengeRating>
{
    public override ChallengeRating Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            var d = reader.GetDouble();
            return ChallengeRating.FromNumeric(d);
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            return ParseFromString(s);
        }

        if (reader.TokenType == JsonTokenType.Null)
            return default;

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            // Handle object shapes that can happen if CR got serialized without this converter.
            // Try common fields: Text/Numeric (or lowercase), and also cr/challenge_rating/value.
            if (TryGetString(root, "Text", out var text) || TryGetString(root, "text", out text))
            {
                if (!string.IsNullOrWhiteSpace(text))
                    return ChallengeRating.FromText(text);
            }

            if (TryGetDouble(root, "Numeric", out var numeric) || TryGetDouble(root, "numeric", out numeric))
            {
                return ChallengeRating.FromNumeric(numeric);
            }

            if (TryGetString(root, "cr", out var crStr) || TryGetString(root, "challenge_rating", out crStr) || TryGetString(root, "value", out crStr))
            {
                return ParseFromString(crStr);
            }

            if (TryGetDouble(root, "cr", out var crNum) || TryGetDouble(root, "challenge_rating", out crNum) || TryGetDouble(root, "value", out crNum))
            {
                return ChallengeRating.FromNumeric(crNum);
            }

            throw new JsonException("Unexpected object shape when parsing ChallengeRating.");
        }

        throw new JsonException($"Unexpected token {reader.TokenType} when parsing ChallengeRating.");
    }

    private static bool TryGetString(JsonElement obj, string prop, out string? value)
    {
        value = null;
        if (!obj.TryGetProperty(prop, out var p)) return false;
        if (p.ValueKind != JsonValueKind.String) return false;
        value = p.GetString();
        return true;
    }

    private static bool TryGetDouble(JsonElement obj, string prop, out double value)
    {
        value = 0;
        if (!obj.TryGetProperty(prop, out var p)) return false;

        if (p.ValueKind == JsonValueKind.Number && p.TryGetDouble(out value)) return true;
        if (p.ValueKind == JsonValueKind.String && double.TryParse(p.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return true;

        return false;
    }

    public override void Write(Utf8JsonWriter writer, ChallengeRating value, JsonSerializerOptions options)
    {
        // Persist as the SRD-friendly display value (e.g. "1/4", "7")
        writer.WriteStringValue(value.ToString());
    }

    private static ChallengeRating ParseFromString(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return default;

        s = s.Trim();

        // Handle double-encoded strings like "\"7\""
        // If it starts/ends with quotes, remove them repeatedly.
        // Also handles cases where the string literal includes escaped quotes.
        s = UnwrapQuotedString(s);

        // Now parse common CR formats: "1/4", "1/2", "2", "7", "0.25"
        if (TryParseFraction(s, out var frac))
            return ChallengeRating.FromNumeric(frac);

        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            return ChallengeRating.FromNumeric(d);

        // Some sources may use "1/8 (25 XP)" or similar — keep first token
        var firstToken = s.Split(' ', '\t', '\r', '\n').FirstOrDefault() ?? s;
        firstToken = UnwrapQuotedString(firstToken);

        if (TryParseFraction(firstToken, out frac))
            return ChallengeRating.FromNumeric(frac);

        if (double.TryParse(firstToken, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
            return ChallengeRating.FromNumeric(d);

        // Last-resort: store raw
        return ChallengeRating.FromText(s);
    }

    private static string UnwrapQuotedString(string s)
    {
        s = s.Trim();

        // If string includes escaped quotes around itself: \"7\"
        if (s.Length >= 4 && s.StartsWith("\\\"") && s.EndsWith("\\\""))
            s = s.Substring(2, s.Length - 4);

        // Repeatedly unwrap "..."
        while (s.Length >= 2 && s.StartsWith("\"") && s.EndsWith("\""))
            s = s.Substring(1, s.Length - 2).Trim();

        // Remove any remaining leading/trailing escaped quotes
        s = s.Trim();
        if (s.StartsWith("\\\"")) s = s.Substring(2);
        if (s.EndsWith("\\\"")) s = s.Substring(0, s.Length - 2);

        return s.Trim();
    }

    private static bool TryParseFraction(string s, out double value)
    {
        value = 0;

        var parts = s.Split('/');
        if (parts.Length != 2) return false;

        if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var num)) return false;
        if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var den)) return false;
        if (den == 0) return false;

        value = num / den;
        return true;
    }
}
