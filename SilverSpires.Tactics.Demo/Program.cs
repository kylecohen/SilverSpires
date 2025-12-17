using SilverSpires.Tactics.Characters;
using SilverSpires.Tactics.Combat;
using SilverSpires.Tactics.Encounters;
using SilverSpires.Tactics.Maps;
using SilverSpires.Tactics.Srd.Persistence.Catalog;
using SilverSpires.Tactics.Srd.Persistence.Storage;
using SilverSpires.Tactics.Srd.Persistence.Storage.SqlServer;
using SilverSpires.Tactics.Srd.Persistence.Storage.Sqlite;
using SilverSpires.Tactics.Srd.Ingestion.Ingestion;
using SilverSpires.Tactics.Srd.Ingestion.Mapping;
using SilverSpires.Tactics.Srd.Ingestion.Sources.Json;
using SilverSpires.Tactics.Srd.IngestionModule.Ingestion;

namespace SilverSpires.Tactics.Demo;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var repo = ISrdRepository.CreateRepository();
        await repo.InitializeAsync();

        // Convenience bootstrap so the demo can update without manual setup.
        // If you want the DB to be totally user-driven, remove this line.
        await Open5eBootstrap.EnsureRegisteredAsync(repo);

        // Update SRD from enabled sources
        var http = new HttpClient();
        var readers = new DefaultSourceReaderFactory(http);
        var mapper = new GenericMappingEngine();
        var ingestion = new SrdIngestionService(repo, readers, mapper);
        var updater = new SrdUpdater(ingestion);
        await updater.UpdateAllEnabledSourcesAsync();

        // Load catalog from DB
        var catalog = new DbSrdCatalog(repo);
        await catalog.LoadAsync();

        Console.WriteLine($"Loaded SRD from DB: Monsters={catalog.Monsters.Count}, Spells={catalog.Spells.Count}");

        // Map
        var map = new GameMap(width: 20, height: 12);

        // PCs (standard array)
        var pc1Tpl = new PlayerCharacterTemplate
        {
            Name = "Player One",
            Strength = 15, Dexterity = 14, Constitution = 13, Intelligence = 12, Wisdom = 10, Charisma = 8,
            WeaponId = "longsword",
            ArmorId = "chain_mail"
        };

        var pc2Tpl = new PlayerCharacterTemplate
        {
            Name = "Player Two",
            Strength = 14, Dexterity = 15, Constitution = 13,
            Intelligence = 10, Wisdom = 12, Charisma = 8,
            WeaponId = "rapier",
            ArmorId = "leather"
        };

        var pc1 = PlayerCharacterFactory.CreateBattleUnit(catalog, pc1Tpl, new GridPosition(2, 2), Faction.Player1);
        var pc2 = PlayerCharacterFactory.CreateBattleUnit(catalog, pc2Tpl, new GridPosition(2, 4), Faction.Player2);

        // Goblins via encounter service (spawn grouped)
        var encounter = EncounterDefinition.Create(
            id: "demo_goblins",
            name: "Demo Goblin Skirmish",
            new EncounterSpawnSpec
            {
                MonsterId = "goblin",
                Count = 4,
                SpawnArea = new RectangleArea(x: 14, y: 2, width: 4, height: 6),
                GroupTag = "goblins"
            });

        var encounterService = new EncounterService(catalog);
        var goblins = encounterService.SpawnEncounter(map, encounter, Faction.Goblins);

        var units = new List<BattleUnit> { pc1, pc2 };
        units.AddRange(goblins);

        // Run battle (simple nearest-enemy AI inside BattleRunner)
        var runner = new BattleRunner();
        var winner = runner.RunBattle(map, units);

        Console.WriteLine();
        Console.WriteLine($"Winner: {winner}");
    }
}
