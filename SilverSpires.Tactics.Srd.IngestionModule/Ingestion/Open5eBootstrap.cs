using System.Text.Json;
using SilverSpires.Tactics.Srd.Ingestion.Sources.Json;
using SilverSpires.Tactics.Srd.Persistence.Registry;
using SilverSpires.Tactics.Srd.Persistence.Storage;

namespace SilverSpires.Tactics.Srd.IngestionModule.Ingestion;

public static class Open5eBootstrap
{
    public static async Task EnsureRegisteredAsync(ISrdRepository repo, CancellationToken ct = default)
    {
        // If already exists, do nothing
        var existing = await repo.GetSourceAsync("open5e", ct);
        if (existing != null) return;

        var source = new SourceDefinition
        {
            Id = "open5e",
            Name = "Open5e",
            Kind = SrdSourceKind.HttpJson,
            ConnectionJson = JsonSerializer.Serialize(new SourceConnection
            {
                BaseUrl = "https://api.open5e.com/",
                Headers = null
            }),
            IsEnabled = true
        };
        await repo.UpsertSourceAsync(source, ct);

        // Create mapping profiles (simple format: property->string rule)
        async Task AddProfile(string id, string name, SrdEntityType et, Dictionary<string,string?> fields)
        {
            var rules = JsonSerializer.Serialize(fields);
            await repo.UpsertMappingProfileAsync(new MappingProfile
            {
                Id = id,
                Name = name,
                EntityType = et,
                RulesJson = rules
            }, ct);
        }

        // Basic defaults: Id uses slug, everything else auto-match ("")
        Dictionary<string,string?> BaseIdSlug() => new(StringComparer.OrdinalIgnoreCase){ ["Id"]="slug" };

        await AddProfile("open5e_monster_default", "Open5e Monster Default", SrdEntityType.Monster, BaseIdSlug());
        await AddProfile("open5e_spell_default", "Open5e Spell Default", SrdEntityType.Spell, BaseIdSlug());
        await AddProfile("open5e_class_default", "Open5e Class Default", SrdEntityType.Class, BaseIdSlug());
        await AddProfile("open5e_race_default", "Open5e Race Default", SrdEntityType.Race, BaseIdSlug());
        await AddProfile("open5e_background_default", "Open5e Background Default", SrdEntityType.Background, BaseIdSlug());
        await AddProfile("open5e_feat_default", "Open5e Feat Default", SrdEntityType.Feat, BaseIdSlug());
        await AddProfile("open5e_magicitem_default", "Open5e MagicItem Default", SrdEntityType.MagicItem, BaseIdSlug());
        await AddProfile("open5e_weapon_default", "Open5e Weapon Default", SrdEntityType.Weapon, BaseIdSlug());
        await AddProfile("open5e_armor_default", "Open5e Armor Default", SrdEntityType.Armor, BaseIdSlug());

        async Task AddFeed(string id, SrdEntityType et, string pathOrUrl, string profileId, string itemsProp="results")
        {
            await repo.UpsertFeedAsync(new SourceEntityFeed
            {
                Id = id,
                SourceId = "open5e",
                EntityType = et,
                MappingProfileId = profileId,
                IsEnabled = true,
                FeedJson = JsonSerializer.Serialize(new JsonFeedConfig
                {
                    PathOrUrl = pathOrUrl,
                    ItemsProperty = itemsProp,
                    NextPageProperty = "next"
                })
            }, ct);
        }

        // based on Open5e endpoints from their root listing:
        await AddFeed("open5e_monsters", SrdEntityType.Monster, "v1/monsters/?format=json", "open5e_monster_default");
        await AddFeed("open5e_spells", SrdEntityType.Spell, "v2/spells/?format=json", "open5e_spell_default");
        await AddFeed("open5e_classes", SrdEntityType.Class, "v1/classes/?format=json", "open5e_class_default");
        await AddFeed("open5e_races", SrdEntityType.Race, "v1/races/?format=json", "open5e_race_default");
        await AddFeed("open5e_backgrounds", SrdEntityType.Background, "v2/backgrounds/?format=json", "open5e_background_default");
        await AddFeed("open5e_feats", SrdEntityType.Feat, "v2/feats/?format=json", "open5e_feat_default");
        await AddFeed("open5e_magicitems", SrdEntityType.MagicItem, "v1/magicitems/?format=json", "open5e_magicitem_default");
        await AddFeed("open5e_weapons", SrdEntityType.Weapon, "v2/weapons/?format=json", "open5e_weapon_default");
        await AddFeed("open5e_armor", SrdEntityType.Armor, "v2/armor/?format=json", "open5e_armor_default");
    }
}
