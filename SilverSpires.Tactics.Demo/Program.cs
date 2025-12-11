using System;
using System.IO;
using System.Linq;
using SilverSpires.Tactics.Characters;
using SilverSpires.Tactics.Combat;
using SilverSpires.Tactics.Encounters;
using SilverSpires.Tactics.Maps;
using SilverSpires.Tactics.Srd.Data;

namespace SilverSpires.Tactics.Demo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var solutionRoot = FindSolutionRoot();
            var srdJsonPath = Path.Combine(
                solutionRoot,
                "SilverSpires.Tactics.Srd",
                "Data",
                "Json");

            ISrdCatalog catalog = new JsonSrdCatalog(srdJsonPath);

            var map = BuildMap(20, 12);

            var pc1Template = new PlayerCharacterTemplate
            {
                Name = "Alaric",
                Level = 1,
                Strength = 16,
                Dexterity = 12,
                Constitution = 14,
                Intelligence = 10,
                Wisdom = 10,
                Charisma = 8
            };

            var pc2Template = new PlayerCharacterTemplate
            {
                Name = "Briala",
                Level = 1,
                Strength = 14,
                Dexterity = 16,
                Constitution = 13,
                Intelligence = 10,
                Wisdom = 12,
                Charisma = 8
            };

            var pc1 = PlayerCharacterFactory.CreateBattleUnit(
                catalog, pc1Template, new GridPosition(2, 5), Faction.Player1);

            var pc2 = PlayerCharacterFactory.CreateBattleUnit(
                catalog, pc2Template, new GridPosition(2, 7), Faction.Player2);

            var encounterService = new EncounterService(catalog, new Random());
            var goblinEncounter = EncounterDefinition.Create(
                id: "demo_goblins",
                name: "Demo Goblin Pack",
                new EncounterSpawnSpec
                {
                    MonsterId = "srd_goblin",
                    Count = 4,
                    SpawnArea = new RectangleArea(13, 4, 5, 4),
                    GroupTag = "goblins"
                });

            var goblinUnits = encounterService
                .SpawnEncounter(map, goblinEncounter, Faction.Goblins)
                .ToList();

            var allUnits = new System.Collections.Generic.List<BattleUnit>();
            allUnits.Add(pc1);
            allUnits.Add(pc2);
            allUnits.AddRange(goblinUnits);

            var battleRunner = new BattleRunner();
            var winner = battleRunner.RunBattle(map, allUnits, new Random());

            Console.WriteLine();
            Console.WriteLine("Final survivors:");
            foreach (var u in allUnits.Where(u => u.IsAlive))
            {
                Console.WriteLine($"  {u}");
            }

            Console.WriteLine();
            Console.WriteLine($"Winner faction: {winner}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static string FindSolutionRoot()
        {
            var dir = Directory.GetCurrentDirectory();
            while (dir != null)
            {
                if (Directory.GetFiles(dir, "*.sln").Length > 0)
                    return dir;

                dir = Directory.GetParent(dir)?.FullName;
            }

            throw new InvalidOperationException("Could not find solution root with a .sln file.");
        }

        private static GameMap BuildMap(int width, int height)
        {
            var map = new GameMap(width, height);

            for (int x = 0; x < map.Width; x++)
            {
                map[x, 0].BlocksMovement = map[x, 0].BlocksVision = true;
                map[x, map.Height - 1].BlocksMovement = map[x, map.Height - 1].BlocksVision = true;
            }
            for (int y = 0; y < map.Height; y++)
            {
                map[0, y].BlocksMovement = map[0, y].BlocksVision = true;
                map[map.Width - 1, y].BlocksMovement = map[map.Width - 1, y].BlocksVision = true;
            }

            return map;
        }
    }
}
