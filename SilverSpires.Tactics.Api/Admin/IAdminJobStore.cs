namespace SilverSpires.Tactics.Api.Admin;

public interface IAdminJobStore
{
    Task InitializeAsync(CancellationToken ct = default);
    Task<AdminJob> CreateQueuedAsync(string type, CancellationToken ct = default);
    Task<AdminJob?> GetAsync(Guid id, CancellationToken ct = default);
    Task UpdateStateAsync(Guid id, string state, DateTime? startedUtc = null, DateTime? completedUtc = null, string? error = null, CancellationToken ct = default);
}
