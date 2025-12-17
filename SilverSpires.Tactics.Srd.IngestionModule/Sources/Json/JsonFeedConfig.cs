namespace SilverSpires.Tactics.Srd.Ingestion.Sources.Json;

public sealed class JsonFeedConfig
{
    // For FileJson: relative path under basePath; For HttpJson: absolute or relative to baseUrl
    public string PathOrUrl { get; set; } = string.Empty;

    // JSON Pointer-like dot path to array (default "results"); supports "" for root array
    public string ItemsProperty { get; set; } = "results";

    // For paged HTTP APIs like Open5e: property that contains next page URL (default "next")
    public string NextPageProperty { get; set; } = "next";
}
