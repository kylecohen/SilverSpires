namespace SilverSpires.Tactics.Srd.Ingestion.Sources.Json;

public sealed class SourceConnection
{
    public string? BasePath { get; set; } // for FileJson
    public string? BaseUrl { get; set; }  // for HttpJson
    public Dictionary<string, string>? Headers { get; set; }
}
