using System.Net.Http.Json;
using System.Text.Json;
using SilverSpires.Tactics.Srd.Characters;
using SilverSpires.Tactics.Srd.Items;
using SilverSpires.Tactics.Srd.Monsters;
using SilverSpires.Tactics.Srd.Persistence.Storage;
using SilverSpires.Tactics.Srd.Rules;
using SilverSpires.Tactics.Srd.Spells;

namespace SilverSpires.Tactics.Sync;

/// <summary>
/// Pulls SRD data from the server API and upserts it into a local repository (typically SQLite cache).
/// This is intentionally backend-agnostic: the caller supplies the local ISrdRepository implementation.
/// </summary>
public sealed class SrdSyncClient
{
    private readonly HttpClient _http;
    private readonly ISrdRepository _localRepo;
    private readonly JsonSerializerOptions _json;

    public SrdSyncClient(HttpClient http, ISrdRepository localRepo, JsonSerializerOptions json)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _localRepo = localRepo ?? throw new ArgumentNullException(nameof(localRepo));
        _json = json ?? throw new ArgumentNullException(nameof(json));
    }

    public async Task<DateTime?> GetServerLatestUpdatedUtcAsync(CancellationToken ct = default)
    {
        var manifest = await _http.GetFromJsonAsync<SrdManifest>("/api/srd/manifest", _json, cancellationToken: ct);
        return manifest?.LatestEntityUpdatedUtc;
    }

    /// <summary>
    /// Sync all supported SRD entity types using the server's raw sync endpoint.
    /// Pass lastSyncUtc as the "updatedSinceUtc" filter. Returns the new lastSyncUtc you should persist.
    /// </summary>
    public async Task<DateTime> SyncAllAsync(DateTime? lastSyncUtc, CancellationToken ct = default)
    {
        await _localRepo.InitializeAsync(ct);

        // If the server doesn't expose a latest timestamp, fall back to "now" after sync.
        var serverLatest = await GetServerLatestUpdatedUtcAsync(ct);

        await SyncTypeAsync("Class", lastSyncUtc, UpsertClassAsync, ct);
        await SyncTypeAsync("Race", lastSyncUtc, UpsertRaceAsync, ct);
        await SyncTypeAsync("Background", lastSyncUtc, UpsertBackgroundAsync, ct);
        await SyncTypeAsync("Feat", lastSyncUtc, UpsertFeatAsync, ct);
        await SyncTypeAsync("Skill", lastSyncUtc, UpsertSkillAsync, ct);
        await SyncTypeAsync("Language", lastSyncUtc, UpsertLanguageAsync, ct);
        await SyncTypeAsync("Spell", lastSyncUtc, UpsertSpellAsync, ct);
        await SyncTypeAsync("Monster", lastSyncUtc, UpsertMonsterAsync, ct);
        await SyncTypeAsync("MagicItem", lastSyncUtc, UpsertMagicItemAsync, ct);
        await SyncTypeAsync("Equipment", lastSyncUtc, UpsertEquipmentAsync, ct);
        await SyncTypeAsync("Weapon", lastSyncUtc, UpsertWeaponAsync, ct);
        await SyncTypeAsync("Armor", lastSyncUtc, UpsertArmorAsync, ct);
        await SyncTypeAsync("Effect", lastSyncUtc, UpsertEffectAsync, ct);

        return serverLatest ?? DateTime.UtcNow;
    }

    private async Task SyncTypeAsync(
        string entityType,
        DateTime? updatedSinceUtc,
        Func<string, Task> upsertFromJson,
        CancellationToken ct)
    {
        const int PageSize = 500;
        var page = 0;

        while (true)
        {
            var url = $"/api/srd/sync/{Uri.EscapeDataString(entityType)}?page={page}&pageSize={PageSize}";
            if (updatedSinceUtc is not null)
                url += $"&updatedSinceUtc={Uri.EscapeDataString(updatedSinceUtc.Value.ToString("o"))}";

            var batch = await _http.GetFromJsonAsync<List<SrdEntityEnvelope>>(url, _json, cancellationToken: ct);
            if (batch is null || batch.Count == 0)
                break;

            foreach (var env in batch)
            {
                // env.Json is the canonical stored JSON for the model.
                await upsertFromJson(env.Json);
            }

            if (batch.Count < PageSize)
                break;

            page++;
        }
    }

    // -----------------
    // Upsert helpers
    // -----------------
    private async Task UpsertClassAsync(string json)
    {
        var e = JsonSerializer.Deserialize<SrdClass>(json, _json);
        if (e is not null) await _localRepo.UpsertClassAsync(e);
    }

    private async Task UpsertRaceAsync(string json)
    {
        var e = JsonSerializer.Deserialize<SrdRace>(json, _json);
        if (e is not null) await _localRepo.UpsertRaceAsync(e);
    }

    private async Task UpsertBackgroundAsync(string json)
    {
        var e = JsonSerializer.Deserialize<SrdBackground>(json, _json);
        if (e is not null) await _localRepo.UpsertBackgroundAsync(e);
    }

    private async Task UpsertFeatAsync(string json)
    {
        var e = JsonSerializer.Deserialize<SrdFeat>(json, _json);
        if (e is not null) await _localRepo.UpsertFeatAsync(e);
    }

    private async Task UpsertSkillAsync(string json)
    {
        var e = JsonSerializer.Deserialize<SrdSkill>(json, _json);
        if (e is not null) await _localRepo.UpsertSkillAsync(e);
    }

    private async Task UpsertLanguageAsync(string json)
    {
        var e = JsonSerializer.Deserialize<SrdLanguage>(json, _json);
        if (e is not null) await _localRepo.UpsertLanguageAsync(e);
    }

    private async Task UpsertSpellAsync(string json)
    {
        var e = JsonSerializer.Deserialize<SrdSpell>(json, _json);
        if (e is not null) await _localRepo.UpsertSpellAsync(e);
    }

    private async Task UpsertMonsterAsync(string json)
    {
        var e = JsonSerializer.Deserialize<SrdMonster>(json, _json);
        if (e is not null) await _localRepo.UpsertMonsterAsync(e);
    }

    private async Task UpsertMagicItemAsync(string json)
    {
        var e = JsonSerializer.Deserialize<SrdMagicItem>(json, _json);
        if (e is not null) await _localRepo.UpsertMagicItemAsync(e);
    }

    private async Task UpsertEquipmentAsync(string json)
    {
        var e = JsonSerializer.Deserialize<SrdEquipment>(json, _json);
        if (e is not null) await _localRepo.UpsertEquipmentAsync(e);
    }

    private async Task UpsertWeaponAsync(string json)
    {
        var e = JsonSerializer.Deserialize<SrdWeapon>(json, _json);
        if (e is not null) await _localRepo.UpsertWeaponAsync(e);
    }

    private async Task UpsertArmorAsync(string json)
    {
        var e = JsonSerializer.Deserialize<SrdArmor>(json, _json);
        if (e is not null) await _localRepo.UpsertArmorAsync(e);
    }

    private async Task UpsertEffectAsync(string json)
    {
        var e = JsonSerializer.Deserialize<GameEffect>(json, _json);
        if (e is not null) await _localRepo.UpsertEffectAsync(e);
    }

    private sealed class SrdManifest
    {
        public DateTime? LatestEntityUpdatedUtc { get; set; }
    }
}
