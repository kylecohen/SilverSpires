using System.Net.Http.Headers;
using System.Text.Json;
using SilverSpires.Tactics.Srd.Ingestion.Abstractions;

namespace SilverSpires.Tactics.Srd.Ingestion.Sources.Json;

public sealed class HttpJsonSourceReader : ISourceReader
{
    private readonly HttpClient _http;

    public HttpJsonSourceReader(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public async IAsyncEnumerable<JsonElement> ReadAsync(SourceEntityFeed feed, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var src = JsonFeedConfigParser.Parse(feed.FeedJson);
        if (string.IsNullOrWhiteSpace(src.Url))
            throw new InvalidOperationException($"Feed {feed.Id} missing 'url'");

        using var resp = await _http.GetAsync(src.Url, ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct);
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
            yield return container;
        }
    }
}
