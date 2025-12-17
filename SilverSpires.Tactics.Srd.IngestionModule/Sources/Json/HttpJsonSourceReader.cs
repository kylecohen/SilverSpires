using System.Text.Json;
using SilverSpires.Tactics.Srd.Ingestion.Abstractions;
using SilverSpires.Tactics.Srd.Persistence.Registry;

namespace SilverSpires.Tactics.Srd.Ingestion.Sources.Json;

public sealed class HttpJsonSourceReader : ISourceReader
{
    private readonly HttpClient _http;

    public HttpJsonSourceReader(HttpClient httpClient)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async IAsyncEnumerable<JsonElement> ReadAsync(SourceDefinition source, SourceEntityFeed feed, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var conn = source.GetConnection<SourceConnection>() ?? new SourceConnection();
        var cfg = feed.GetFeed<JsonFeedConfig>() ?? new JsonFeedConfig();

        if (string.IsNullOrWhiteSpace(cfg.PathOrUrl))
            yield break;

        var url = BuildUrl(conn.BaseUrl, cfg.PathOrUrl);
        var headers = conn.Headers;

        while (!string.IsNullOrWhiteSpace(url))
        {
            ct.ThrowIfCancellationRequested();

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            if (headers != null)
            {
                foreach (var kv in headers)
                    req.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
            }

            using var resp = await _http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            foreach (var item in ExtractItems(root, cfg.ItemsProperty))
            {
                ct.ThrowIfCancellationRequested();
                yield return item;
            }

            url = ExtractNext(root, cfg.NextPageProperty);
        }
    }

    private static string BuildUrl(string? baseUrl, string pathOrUrl)
    {
        if (Uri.TryCreate(pathOrUrl, UriKind.Absolute, out var abs))
            return abs.ToString();

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("HttpJson source missing BaseUrl in ConnectionJson and feed PathOrUrl is not absolute.");

        return new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), pathOrUrl.TrimStart('/')).ToString();
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

    private static string? ExtractNext(JsonElement root, string nextProp)
    {
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(nextProp, out var next))
        {
            if (next.ValueKind == JsonValueKind.String)
                return next.GetString();
            if (next.ValueKind == JsonValueKind.Null)
                return null;
        }
        return null;
    }
}
