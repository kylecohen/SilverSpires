using System.Text.Json;

namespace SilverSpires.Tactics.Srd.Ingestion.Sources.Json;

/// <summary>
/// FeedJson schema for both file and http feeds.
/// </summary>
public sealed class JsonFeedConfig
{
    public string? Path { get; set; }           // for FileJson
    public string? Url { get; set; }            // for HttpJson
    public string Root { get; set; } = "$";     // "$" or "$.results"
}

public static class JsonFeedConfigParser
{
    public static JsonFeedConfig Parse(string feedJson)
        => JsonSerializer.Deserialize<JsonFeedConfig>(feedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
           ?? new JsonFeedConfig();
}
