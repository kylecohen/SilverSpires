using System.Text.Json;
using SilverSpires.Tactics.Srd.Ingestion.Abstractions;
using SilverSpires.Tactics.Srd.Persistence.Registry;

namespace SilverSpires.Tactics.Srd.Ingestion.Sources.Json;

public sealed class FileJsonSourceReader : ISourceReader
{
    public async IAsyncEnumerable<JsonElement> ReadAsync(SourceDefinition source, SourceEntityFeed feed, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var conn = source.GetConnection<SourceConnection>() ?? new SourceConnection();
        var cfg = feed.GetFeed<JsonFeedConfig>() ?? new JsonFeedConfig();

        if (string.IsNullOrWhiteSpace(conn.BasePath))
            throw new InvalidOperationException($"Source '{source.Id}' missing ConnectionJson.BasePath for FileJson");

        var fullPath = Path.Combine(conn.BasePath!, cfg.PathOrUrl);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException(fullPath);

        var json = await File.ReadAllTextAsync(fullPath, ct);
        using var doc = JsonDocument.Parse(json);

        foreach (var item in ExtractItems(doc.RootElement, cfg.ItemsProperty))
        {
            ct.ThrowIfCancellationRequested();
            yield return item;
        }
    }

    private static IEnumerable<JsonElement> ExtractItems(JsonElement root, string itemsProp)
    {
        if (string.IsNullOrWhiteSpace(itemsProp))
        {
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in root.EnumerateArray())
                    yield return el;
            }
            yield break;
        }

        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(itemsProp, out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in arr.EnumerateArray())
                yield return el;
        }
    }
}
