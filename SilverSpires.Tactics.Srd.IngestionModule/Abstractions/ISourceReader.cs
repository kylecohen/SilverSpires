using System.Text.Json;

namespace SilverSpires.Tactics.Srd.Ingestion.Abstractions;

/// <summary>
/// A reader produces raw JSON objects for an entity feed. No SRD assumptions.
/// </summary>
public interface ISourceReader
{
    IAsyncEnumerable<JsonElement> ReadAsync(SourceEntityFeed feed, CancellationToken ct = default);
}

/// <summary>
/// Builds a reader based on a feed's source kind.
/// </summary>
public interface ISourceReaderFactory
{
    ISourceReader Create(SrdSourceKind kind);
}
