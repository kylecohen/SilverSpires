using System.Text.Json;

namespace SilverSpires.Tactics.Srd.Ingestion.Mapping;

/// <summary>
/// Simple mapping profile:
/// - Each SRD property name is a JSON string.
/// - "" or null -> auto-match (same name or synonym)
/// - "NA" -> skip mapping (explicitly not available)
/// - otherwise -> source field name (or dot path)
/// </summary>
public sealed class SimpleFieldMap
{
    public const string NotAvailableToken = "NA";

    public Dictionary<string, string?> Fields { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static SimpleFieldMap FromJson(string json)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        if (doc.RootElement.ValueKind != JsonValueKind.Object) return new SimpleFieldMap();

        var map = new SimpleFieldMap();
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            map.Fields[prop.Name] = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() : null;
        }
        return map;
    }
}
