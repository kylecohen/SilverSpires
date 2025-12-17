using System.Text.Json;
using SilverSpires.Tactics.Srd.Ingestion.Abstractions;
using SilverSpires.Tactics.Srd.Ingestion.Mapping;
using SilverSpires.Tactics.Srd.Persistence.Registry;
using SilverSpires.Tactics.Srd.Persistence.Storage;
using SilverSpires.Tactics.Srd.Characters;
using SilverSpires.Tactics.Srd.Items;
using SilverSpires.Tactics.Srd.Monsters;
using SilverSpires.Tactics.Srd.Rules;
using SilverSpires.Tactics.Srd.Spells;

namespace SilverSpires.Tactics.Srd.Ingestion.Ingestion;

public sealed class SrdIngestionService
{
    private readonly ISrdRepository _repo;
    private readonly ISourceReaderFactory _readers;
    private readonly IMappingEngine _mapper;

    public SrdIngestionService(ISrdRepository repo, ISourceReaderFactory readers, IMappingEngine mapper)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _readers = readers ?? throw new ArgumentNullException(nameof(readers));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<IngestionReport> IngestAllEnabledSourcesAsync(string sourceVersion = "unknown", CancellationToken ct = default)
    {
        var report = new IngestionReport();
        var sources = await _repo.GetSourcesAsync(ct);

        foreach (var s in sources.Where(x => x.IsEnabled))
        {
            var sub = await IngestSourceAsync(s.Id, sourceVersion, ct);
            report.Read += sub.Read;
            report.Upserted += sub.Upserted;
            report.Skipped += sub.Skipped;
            report.Warnings.AddRange(sub.Warnings);
            report.Errors.AddRange(sub.Errors);
        }

        return report;
    }

    public async Task<IngestionReport> IngestSourceAsync(string sourceId, string sourceVersion, CancellationToken ct = default)
    {
        var report = new IngestionReport();

        var source = await _repo.GetSourceAsync(sourceId, ct)
            ?? throw new InvalidOperationException($"Unknown source '{sourceId}'");

        var feeds = await _repo.GetFeedsBySourceAsync(sourceId, enabledOnly: true, ct);

        foreach (var feed in feeds)
        {
            ct.ThrowIfCancellationRequested();
            var sub = await IngestFeedAsync(source, feed, sourceVersion, ct);
            report.Read += sub.Read;
            report.Upserted += sub.Upserted;
            report.Skipped += sub.Skipped;
            report.Warnings.AddRange(sub.Warnings);
            report.Errors.AddRange(sub.Errors);
        }

        return report;
    }

    public async Task<IngestionReport> IngestFeedAsync(SourceDefinition source, SourceEntityFeed feed, string sourceVersion, CancellationToken ct = default)
    {
        var report = new IngestionReport();

        var profile = await _repo.GetMappingProfileAsync(feed.MappingProfileId, ct)
            ?? throw new InvalidOperationException($"Feed '{feed.Id}' references missing mapping profile '{feed.MappingProfileId}'");

        var reader = _readers.Create(source.Kind);
        var meta = new SrdSourceMetadata(source.Id, sourceVersion, DateTime.UtcNow);

        await foreach (var obj in reader.ReadAsync(source, feed, ct))
        {
            report.Read++;

            switch (feed.EntityType)
            {
                case SrdEntityType.Monster:
                    await MapAndUpsert<SrdMonster>(obj, profile, meta, _repo.UpsertMonsterAsync, report, ct);
                    break;
                case SrdEntityType.Spell:
                    await MapAndUpsert<SrdSpell>(obj, profile, meta, _repo.UpsertSpellAsync, report, ct);
                    break;
                case SrdEntityType.Class:
                    await MapAndUpsert<SrdClass>(obj, profile, meta, _repo.UpsertClassAsync, report, ct);
                    break;
                case SrdEntityType.Race:
                    await MapAndUpsert<SrdRace>(obj, profile, meta, _repo.UpsertRaceAsync, report, ct);
                    break;
                case SrdEntityType.Background:
                    await MapAndUpsert<SrdBackground>(obj, profile, meta, _repo.UpsertBackgroundAsync, report, ct);
                    break;
                case SrdEntityType.Feat:
                    await MapAndUpsert<SrdFeat>(obj, profile, meta, _repo.UpsertFeatAsync, report, ct);
                    break;
                case SrdEntityType.Skill:
                    await MapAndUpsert<SrdSkill>(obj, profile, meta, _repo.UpsertSkillAsync, report, ct);
                    break;
                case SrdEntityType.Language:
                    await MapAndUpsert<SrdLanguage>(obj, profile, meta, _repo.UpsertLanguageAsync, report, ct);
                    break;
                case SrdEntityType.MagicItem:
                    await MapAndUpsert<SrdMagicItem>(obj, profile, meta, _repo.UpsertMagicItemAsync, report, ct);
                    break;
                case SrdEntityType.Equipment:
                    await MapAndUpsert<SrdEquipment>(obj, profile, meta, _repo.UpsertEquipmentAsync, report, ct);
                    break;
                case SrdEntityType.Weapon:
                    await MapAndUpsert<SrdWeapon>(obj, profile, meta, _repo.UpsertWeaponAsync, report, ct);
                    break;
                case SrdEntityType.Armor:
                    await MapAndUpsert<SrdArmor>(obj, profile, meta, _repo.UpsertArmorAsync, report, ct);
                    break;
                case SrdEntityType.Effect:
                    await MapAndUpsert<GameEffect>(obj, profile, meta, _repo.UpsertEffectAsync, report, ct);
                    break;
                default:
                    report.Skipped++;
                    report.Warnings.Add($"Unhandled entity type: {feed.EntityType}");
                    break;
            }
        }

        return report;
    }

    private async Task MapAndUpsert<T>(
        JsonElement obj,
        MappingProfile profile,
        SrdSourceMetadata meta,
        Func<T, CancellationToken, Task> upsert,
        IngestionReport report,
        CancellationToken ct)
    {
        var mapped = _mapper.Map<T>(obj, profile, meta);

        report.Warnings.AddRange(mapped.Warnings);
        report.Errors.AddRange(mapped.Errors);

        if (!mapped.IsSuccess || mapped.Entity is null)
        {
            report.Skipped++;
            return;
        }

        await upsert(mapped.Entity, ct);
        report.Upserted++;
    }
}
