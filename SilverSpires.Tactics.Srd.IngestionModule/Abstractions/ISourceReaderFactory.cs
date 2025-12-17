using SilverSpires.Tactics.Srd.Persistence.Registry;

namespace SilverSpires.Tactics.Srd.Ingestion.Abstractions;

public interface ISourceReaderFactory
{
    ISourceReader Create(SrdSourceKind kind);
}
