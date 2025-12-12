using SilverSpires.Tactics.Srd.Ingestion.Abstractions;

namespace SilverSpires.Tactics.Srd.Ingestion.Sources.Json;

public sealed class DefaultSourceReaderFactory : ISourceReaderFactory
{
    private readonly HttpClient _http;

    public DefaultSourceReaderFactory(HttpClient? http = null)
    {
        _http = http ?? new HttpClient();
    }

    public ISourceReader Create(SrdSourceKind kind)
        => kind switch
        {
            SrdSourceKind.FileJson => new FileJsonSourceReader(),
            SrdSourceKind.HttpJson => new HttpJsonSourceReader(_http),
            _ => throw new NotSupportedException($"Unsupported source kind: {kind}")
        };
}
