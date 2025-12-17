namespace SilverSpires.Tactics.Srd.Data;

public interface ISrdUpdater
{
    Task UpdateAllEnabledSourcesAsync(CancellationToken ct = default);
}
