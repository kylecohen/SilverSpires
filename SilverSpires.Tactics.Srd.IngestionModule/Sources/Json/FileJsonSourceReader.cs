using System.Text.Json;
using SilverSpires.Tactics.Srd.Ingestion.Abstractions;

namespace SilverSpires.Tactics.Srd.Ingestion.Sources.Json;

public sealed class FileJsonSourceReader : ISourceReader
{
    public async IAsyncEnumerable<JsonElement> ReadAsync(SourceEntityFeed feed, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var src = JsonFeedConfigParser.Parse(feed.FeedJson);
        if (string.IsNullOrWhiteSpace(src.Path))
            throw new InvalidOperationException($"Feed {feed.Id} missing 'path'");

        if (!File.Exists(src.Path))
            throw new FileNotFoundException(src.Path);

        var json = await File.ReadAllTextAsync(src.Path, ct);
        using var doc = JsonDocument.Parse(json);

        foreach (var item in ExtractItems(doc.RootElement, src.Root))
        {
            ct.ThrowIfCancellationRequested();
            yield return item;
        }
    }

    private static IEnumerable<JsonElement> ExtractItems(JsonElement root, string jsonPath)
    {
        // Very simple "$" or "$.prop" support.
        JsonElement container = root;
        if (jsonPath != "$")
        {
            var segs = jsonPath.Trim().TrimStart('$').TrimStart('.').Split('.', StringSplitOptions.RemoveEmptyEntries);
            foreach (var seg in segs)
            {
                if (container.ValueKind != JsonValueKind.Object || !container.TryGetProperty(seg, out var next))
                    yield break;
                container = next;
            }
        }

        if (container.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in container.EnumerateArray())
                yield return el;
        }
        else if (container.ValueKind == JsonValueKind.Object)
        {
            // treat as single object
            yield return container;
        }
    }
}
