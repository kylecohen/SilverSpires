using SilverSpires.Tactics.Srd.Data;
using SilverSpires.Tactics.Srd.Ingestion.Ingestion;

namespace SilverSpires.Tactics.Srd.IngestionModule.Ingestion;

public sealed class SrdUpdater : ISrdUpdater
{
    private readonly SrdIngestionService _ingestion;

    public SrdUpdater(SrdIngestionService ingestion)
    {
        _ingestion = ingestion ?? throw new ArgumentNullException(nameof(ingestion));
    }

    public Task UpdateAllEnabledSourcesAsync(CancellationToken ct = default)
        => _ingestion.IngestAllEnabledSourcesAsync("auto", ct);
}
