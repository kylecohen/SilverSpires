using SilverSpires.Tactics.Api.Admin;
using SilverSpires.Tactics.Srd.Data;
using SilverSpires.Tactics.Srd.IngestionModule.Ingestion;
using SilverSpires.Tactics.Srd.Persistence.Storage;

namespace SilverSpires.Tactics.Api;

public sealed class RefreshSrdJobManager
{
    private readonly ISrdRepository _repo;
    private readonly ISrdUpdater _updater;
    private readonly IAdminJobStore _jobs;

    public RefreshSrdJobManager(ISrdRepository repo, ISrdUpdater updater, IAdminJobStore jobs)
    {
        _repo = repo;
        _updater = updater;
        _jobs = jobs;
    }

    public Task InitializeAsync(CancellationToken ct = default)
        => _jobs.InitializeAsync(ct);

    public Task<AdminJob> CreateQueuedAsync(CancellationToken ct = default)
        => _jobs.CreateQueuedAsync("refresh-srd", ct);

    public Task<AdminJob?> GetAsync(Guid id, CancellationToken ct = default)
        => _jobs.GetAsync(id, ct);

    public async Task RunAsync(Guid jobId, CancellationToken ct = default)
    {
        try
        {
            await _jobs.UpdateStateAsync(jobId, "running", startedUtc: DateTime.UtcNow, ct: ct);

            await _repo.InitializeAsync(ct);
            await Open5eBootstrap.EnsureRegisteredAsync(_repo, ct);

            await _updater.UpdateAllEnabledSourcesAsync(ct);

            await _jobs.UpdateStateAsync(jobId, "completed", completedUtc: DateTime.UtcNow, ct: ct);
        }
        catch (Exception ex)
        {
            await _jobs.UpdateStateAsync(jobId, "failed", completedUtc: DateTime.UtcNow, error: ex.ToString(), ct: ct);
        }
    }
}
