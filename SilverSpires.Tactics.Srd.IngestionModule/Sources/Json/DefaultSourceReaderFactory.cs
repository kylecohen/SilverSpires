using SilverSpires.Tactics.Srd.Ingestion.Abstractions;
using SilverSpires.Tactics.Srd.Persistence.Registry;

namespace SilverSpires.Tactics.Srd.Ingestion.Sources.Json;

public sealed class DefaultSourceReaderFactory : ISourceReaderFactory
{
    private readonly HttpClient _http;

    public DefaultSourceReaderFactory(HttpClient httpClient)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public ISourceReader Create(SrdSourceKind kind)
        => kind switch
        {
            SrdSourceKind.FileJson => new FileJsonSourceReader(),
            SrdSourceKind.HttpJson => new HttpJsonSourceReader(_http),
            _ => throw new NotSupportedException($"Unsupported source kind: {kind}")
        };
}
