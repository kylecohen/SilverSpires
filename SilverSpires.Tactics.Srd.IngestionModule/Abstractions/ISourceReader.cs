using System.Text.Json;
using SilverSpires.Tactics.Srd.Persistence.Registry;

namespace SilverSpires.Tactics.Srd.Ingestion.Abstractions;

public interface ISourceReader
{
    IAsyncEnumerable<JsonElement> ReadAsync(SourceDefinition source, SourceEntityFeed feed, CancellationToken ct = default);
}
