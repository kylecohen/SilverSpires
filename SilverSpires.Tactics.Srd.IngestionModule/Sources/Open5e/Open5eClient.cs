using System.Net.Http.Json;
using System.Text.Json;

namespace SilverSpires.Tactics.Srd.Ingestion.Sources.Open5e;

/// <summary>
/// Minimal Open5e client with pagination.
/// Base URL default: https://api.open5e.com/
/// </summary>
public sealed class Open5eClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json;

    public Uri BaseUri { get; }

    public Open5eClient(HttpClient http, string baseUrl = "https://api.open5e.com/")
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        BaseUri = new Uri(baseUrl, UriKind.Absolute);

        _json = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<IReadOnlyList<Open5eMonsterDto>> GetAllMonstersAsync(CancellationToken ct = default)
    {
        var all = new List<Open5eMonsterDto>();

        string? url = new Uri(BaseUri, "monsters/").ToString();

        while (!string.IsNullOrWhiteSpace(url))
        {
            var resp = await _http.GetFromJsonAsync<Open5eListResponse<Open5eMonsterDto>>(url, _json, ct);
            if (resp == null) break;

            all.AddRange(resp.Results);
            url = resp.Next;
        }

        return all;
    }
}
