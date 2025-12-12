namespace SilverSpires.Tactics.Srd.Ingestion.Abstractions;

public interface ISrdMapper<in TSource, TSrd>
{
    MappingResult<TSrd> Map(TSource source, SrdSourceMetadata metadata);
}
