using System.Text.Json;

namespace SilverSpires.Tactics.Srd.Persistence.Storage;

/// <summary>
/// Raw storage envelope for SRD entities (used for sync/paging without forcing model deserialization on the server).
/// </summary>
public sealed record SrdEntityEnvelope(
    string EntityType,
    string Id,
    string Json,
    DateTime UpdatedUtc
);
