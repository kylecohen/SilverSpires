using System.Text.Json;
using SilverSpires.Tactics.Srd.Ingestion.Abstractions;
using SilverSpires.Tactics.Srd.Persistence.Registry;

namespace SilverSpires.Tactics.Srd.Ingestion.Mapping;

public interface IMappingEngine
{
    MappingResult<T> Map<T>(JsonElement sourceObj, MappingProfile profile, SrdSourceMetadata meta);
}
