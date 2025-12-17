namespace SilverSpires.Tactics.Srd.Ingestion.Abstractions;

public sealed record SrdSourceMetadata(string SourceId, string SourceVersion, DateTime ImportedUtc);
