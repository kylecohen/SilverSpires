using System.Text.Json;

namespace SilverSpires.Tactics.Srd.Ingestion.Abstractions;

public interface IMappingEngine
{
    MappingResult<TTarget> Map<TTarget>(
        JsonElement sourceObject,
        MappingProfile profile,
        SrdSourceMetadata metadata);
}
