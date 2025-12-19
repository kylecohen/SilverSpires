namespace SilverSpires.Tactics.Api.Admin;

public sealed record AdminJob(
    Guid Id,
    string Type,
    string State,
    DateTime CreatedUtc,
    DateTime? StartedUtc,
    DateTime? CompletedUtc,
    string? Error
);
