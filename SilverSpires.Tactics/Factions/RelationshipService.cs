using System.Collections.Concurrent;

namespace SilverSpires.Tactics.Factions;

public sealed class RelationshipService : IRelationshipService, IHostilityResolver
{
    private readonly IFactionRepository _repo;

    // caches
    private readonly ConcurrentDictionary<(Guid a, Guid b), int> _factionRelCache = new();
    private readonly ConcurrentDictionary<(Guid a, Guid b), int> _personalRelCache = new();
    private readonly ConcurrentDictionary<Guid, CharacterFactionMembershipRecord[]> _memberCache = new();

    public RelationshipService(IFactionRepository repo)
    {
        _repo = repo;
    }

    public RelationshipBand GetBand(int score) => RelationshipBands.ToBand(score);

    public async Task<int> GetFactionRelationScoreAsync(Guid sourceFactionId, Guid targetFactionId, CancellationToken ct = default)
    {
        if (sourceFactionId == targetFactionId) return 3;

        if (_factionRelCache.TryGetValue((sourceFactionId, targetFactionId), out var cached))
            return cached;

        int score;
        var direct = await _repo.GetFactionRelationOverrideAsync(sourceFactionId, targetFactionId, ct);
        if (direct.HasValue)
        {
            score = RelationshipBands.Clamp(direct.Value);
        }
        else
        {
            var reverse = await _repo.GetFactionRelationOverrideAsync(targetFactionId, sourceFactionId, ct);
            if (reverse.HasValue)
                score = RelationshipBands.Clamp((int)Math.Round(reverse.Value * 0.75));
            else
                score = 0;
        }

        _factionRelCache[(sourceFactionId, targetFactionId)] = score;
        return score;
    }

    private async Task<CharacterFactionMembershipRecord[]> GetMembershipsCachedAsync(Guid characterId, CancellationToken ct)
    {
        if (_memberCache.TryGetValue(characterId, out var cached)) return cached;

        var list = await _repo.GetCharacterFactionsAsync(characterId, ct);
        var arr = list.ToArray();
        _memberCache[characterId] = arr;
        return arr;
    }

    public async Task<int> ComputeFactionInfluenceAsync(Guid fromCharacterId, Guid toCharacterId, CancellationToken ct = default)
    {
        var fa = await GetMembershipsCachedAsync(fromCharacterId, ct);
        var fb = await GetMembershipsCachedAsync(toCharacterId, ct);

        if (fa.Length == 0 || fb.Length == 0) return 0;

        double totalW = 0;
        double sum = 0;

        foreach (var ma in fa)
        {
            var wa = (ma.IsPrimary ? 1.0 : 0.6) * (1.0 + Math.Min(ma.Rank, 10) * 0.05);
            foreach (var mb in fb)
            {
                var wb = (mb.IsPrimary ? 1.0 : 0.6) * (1.0 + Math.Min(mb.Rank, 10) * 0.05);
                var w = wa * wb;
                var fr = await GetFactionRelationScoreAsync(ma.FactionId, mb.FactionId, ct);
                sum += fr * w;
                totalW += w;
            }
        }

        if (totalW <= 0.0001) return 0;

        var avg = sum / totalW;
        var damped = avg * 0.65;
        return RelationshipBands.Clamp((int)Math.Round(damped));
    }

    private async Task<int> GetPersonalCachedAsync(Guid from, Guid to, CancellationToken ct)
    {
        if (_personalRelCache.TryGetValue((from, to), out var cached)) return cached;
        var v = await _repo.GetPersonalRelationshipAsync(from, to, ct);
        var score = RelationshipBands.Clamp(v ?? 0);
        _personalRelCache[(from, to)] = score;
        return score;
    }

    public async Task<int> ComputeFinalPersonalRelationshipAsync(Guid fromCharacterId, Guid toCharacterId, CancellationToken ct = default)
    {
        var pr = await GetPersonalCachedAsync(fromCharacterId, toCharacterId, ct);
        var fi = await ComputeFactionInfluenceAsync(fromCharacterId, toCharacterId, ct);

        var prAbs = Math.Abs(pr);
        var factionWeight = 1.0 - (prAbs / 15.0);
        factionWeight = 0.75 * factionWeight;

        var combined = pr + fi * factionWeight;
        return RelationshipBands.Clamp((int)Math.Round(combined));
    }

    public bool AreHostile(Guid factionA, Guid factionB)
    {
        if (factionA == factionB) return false;
        // Synchronous wrapper: this method is intended for combat hot paths.
        // Use GetFactionRelationScoreAsync if you need live DB reads.
        var key = (factionA, factionB);
        if (_factionRelCache.TryGetValue(key, out var cached))
            return RelationshipBands.ToBand(cached) is RelationshipBand.Hostile or RelationshipBand.Enemy;

        // If not cached yet, treat as hostile by default in combat and let higher-level systems pre-warm cache.
        return true;
    }

    public void InvalidateFactionRelation(Guid a, Guid b)
    {
        _factionRelCache.TryRemove((a, b), out _);
    }

    public void InvalidateCharacter(Guid id)
    {
        _memberCache.TryRemove(id, out _);
        foreach (var k in _personalRelCache.Keys.Where(k => k.a == id || k.b == id).ToList())
            _personalRelCache.TryRemove(k, out _);
    }
}
