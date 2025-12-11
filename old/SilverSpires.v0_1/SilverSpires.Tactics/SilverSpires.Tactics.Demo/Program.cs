using System;
using System.IO;
using System.Linq;
using SilverSpires.Tactics.Core;
using SilverSpires.Tactics.Rules;
using SilverSpires.Tactics.World;
using SilverSpires.Tactics.Characters;
using SilverSpires.Tactics.Export;

namespace SilverSpires.Tactics.Demo
{
    internal static class Program
    {
        private static readonly string[] RaceNames = { "Human", "Elf", "Dwarf", "Halfling" };
        private static readonly string[] ClassNames = { "Fighter", "Rogue", "Wizard", "Cleric" };
        private static readonly string[] BackgroundNames = { "Soldier", "Acolyte", "Criminal", "Sage" };

        private static void Main(string[] args)
        {
            Console.WriteLine("SilverSpires Tactics Demo – Random L1 Duel");

            var rng = new Random();

            // Build minimal world/region/location
            var world = new World.World { Name = "Demo World" };
            var region = new Region { Name = "Demo Region" };
            world.Regions.Add(region);
            var location = new Location(region) { Name = "Demo Location" };
            region.Locations.Add(location);

            var encounter = new Encounter(location) { Name = "Random Level 1 Duel" };

            // Generate two random characters
            var (pc1, weapon1) = CreateRandomCharacter("Challenger 1", rng);
            var (pc2, weapon2) = CreateRandomCharacter("Challenger 2", rng);

            encounter.Combatants.Add(pc1);
            encounter.Combatants.Add(pc2);

            // Export character sheets to text files
            Directory.CreateDirectory("Output");
            var sheet1Path = Path.Combine("Output", $"{SanitizeFileName(pc1.Name)}_CharacterSheet.txt");
            var sheet2Path = Path.Combine("Output", $"{SanitizeFileName(pc2.Name)}_CharacterSheet.txt");

            File.WriteAllText(sheet1Path, TextExporter.CharacterSheet(pc1));
            File.WriteAllText(sheet2Path, TextExporter.CharacterSheet(pc2));

            Console.WriteLine($"Character sheets written to:");
            Console.WriteLine($"  {sheet1Path}");
            Console.WriteLine($"  {sheet2Path}");
            Console.WriteLine();

            var rulesEngine = new DefaultRulesEngine(new RandomRollService(seed: 1234));
            var combatEngine = rulesEngine.CreateCombatEngine(encounter);
            combatEngine.StartEncounter();

            Console.WriteLine("=== Duel Begins ===");
            PrintHp(pc1);
            PrintHp(pc2);

            var context = new RulesContext(encounter, rulesEngine.RollService);

            int round = 1;
            bool combatOver = false;
            int maxRounds = 50;

            while (!combatOver && round <= maxRounds)
            {
                Console.WriteLine($"\n=== Round {round} ===");

                // pc1's turn
                TakeAttackTurn(pc1, pc2, weapon1, context);
                if (IsDown(pc2))
                {
                    combatOver = true;
                    break;
                }

                // pc2's turn
                TakeAttackTurn(pc2, pc1, weapon2, context);
                if (IsDown(pc1))
                {
                    combatOver = true;
                    break;
                }

                combatEngine.ApplyOngoingEffects();
                round++;
            }

            Console.WriteLine("\n=== Duel Ends ===");
            PrintHp(pc1);
            PrintHp(pc2);

            if (IsDown(pc1) && IsDown(pc2))
            {
                Console.WriteLine("Result: Double knockout!");
            }
            else if (IsDown(pc1))
            {
                Console.WriteLine($"Result: {pc2.Name} wins!");
            }
            else if (IsDown(pc2))
            {
                Console.WriteLine($"Result: {pc1.Name} wins!");
            }
            else
            {
                Console.WriteLine("Result: Reached max rounds without a knockout.");
            }
        }

        private static (PlayerCharacter pc, Weapon weapon) CreateRandomCharacter(string defaultName, Random rng)
        {
            var raceName = RaceNames[rng.Next(RaceNames.Length)];
            var className = ClassNames[rng.Next(ClassNames.Length)];
            var backgroundName = BackgroundNames[rng.Next(BackgroundNames.Length)];

            var race = new CharacterRace { Name = raceName };
            var cls = new CharacterClass { Name = className, HitDie = GetHitDieForClass(className) };
            var bg = new CharacterBackground { Name = backgroundName };

            var pc = new PlayerCharacter
            {
                Name = defaultName,
                Level = 1,
                Race = race,
                Class = cls,
                Background = bg,
                Alignment = Alignment.TrueNeutral
            };

            // Standard array
            int[] standardArray = { 15, 14, 13, 12, 10, 8 };
            var priorities = GetAbilityPriorityForClass(className);

            for (int i = 0; i < priorities.Length; i++)
            {
                pc.AbilityScores.SetScore(priorities[i], standardArray[i]);
            }

            // Simple HP: HitDie + Con mod
            int conMod = pc.AbilityScores[AbilityScoreType.Constitution].Modifier;
            int hpMax = cls.HitDie + conMod;
            if (hpMax < 1) hpMax = 1;
            pc.AddResource(new ResourcePool(ResourceType.HitPoints, "HP", hpMax));

            // Basic starting weapon
            var weapon = CreateStartingWeaponForClass(className);

            // Add weapon to inventory
            pc.Inventory.Add(weapon);

            return (pc, weapon);
        }

        private static int GetHitDieForClass(string className) => className switch
        {
            "Fighter" => 10,
            "Rogue" => 8,
            "Wizard" => 6,
            "Cleric" => 8,
            _ => 8
        };

        private static AbilityScoreType[] GetAbilityPriorityForClass(string className) => className switch
        {
            "Fighter" => new[]
            {
                AbilityScoreType.Strength,
                AbilityScoreType.Constitution,
                AbilityScoreType.Dexterity,
                AbilityScoreType.Wisdom,
                AbilityScoreType.Charisma,
                AbilityScoreType.Intelligence
            },
            "Rogue" => new[]
            {
                AbilityScoreType.Dexterity,
                AbilityScoreType.Constitution,
                AbilityScoreType.Intelligence,
                AbilityScoreType.Wisdom,
                AbilityScoreType.Charisma,
                AbilityScoreType.Strength
            },
            "Wizard" => new[]
            {
                AbilityScoreType.Intelligence,
                AbilityScoreType.Constitution,
                AbilityScoreType.Dexterity,
                AbilityScoreType.Wisdom,
                AbilityScoreType.Charisma,
                AbilityScoreType.Strength
            },
            "Cleric" => new[]
            {
                AbilityScoreType.Wisdom,
                AbilityScoreType.Constitution,
                AbilityScoreType.Strength,
                AbilityScoreType.Dexterity,
                AbilityScoreType.Charisma,
                AbilityScoreType.Intelligence
            },
            _ => new[]
            {
                AbilityScoreType.Strength,
                AbilityScoreType.Constitution,
                AbilityScoreType.Dexterity,
                AbilityScoreType.Wisdom,
                AbilityScoreType.Charisma,
                AbilityScoreType.Intelligence
            }
        };

        private static Weapon CreateStartingWeaponForClass(string className) => className switch
        {
            "Fighter" => new Weapon
            {
                Name = "Longsword",
                DamageDice = "1d8",
                DamageType = DamageType.Slashing,
                IsFinesse = false,
                IsRanged = false
            },
            "Rogue" => new Weapon
            {
                Name = "Shortsword",
                DamageDice = "1d6",
                DamageType = DamageType.Piercing,
                IsFinesse = true,
                IsRanged = false
            },
            "Wizard" => new Weapon
            {
                Name = "Quarterstaff",
                DamageDice = "1d6",
                DamageType = DamageType.Bludgeoning,
                IsFinesse = false,
                IsRanged = false
            },
            "Cleric" => new Weapon
            {
                Name = "Mace",
                DamageDice = "1d6",
                DamageType = DamageType.Bludgeoning,
                IsFinesse = false,
                IsRanged = false
            },
            _ => new Weapon
            {
                Name = "Club",
                DamageDice = "1d4",
                DamageType = DamageType.Bludgeoning,
                IsFinesse = false,
                IsRanged = false
            }
        };

        private static void TakeAttackTurn(ICombatant attacker, ICombatant defender, Weapon weapon, RulesContext context)
        {
            var scores = attacker.AbilityScores;
            int strMod = scores[AbilityScoreType.Strength].Modifier;
            int dexMod = scores[AbilityScoreType.Dexterity].Modifier;
            int abilityMod = (weapon.IsRanged || weapon.IsFinesse) ? dexMod : strMod;
            int attackModifier = abilityMod + attacker.ProficiencyBonus;

            var roll = context.RollService.RollD20(AdvantageState.Normal, attackModifier,
                $"Attack: {attacker.Name} -> {defender.Name} with {weapon.Name}");

            Console.WriteLine($"{attacker.Name} attacks {defender.Name} with {weapon.Name}.");
            Console.WriteLine($"  Attack roll: d20 {(roll.AdvantageState == AdvantageState.Advantage ? "(adv) " : roll.AdvantageState == AdvantageState.Disadvantage ? "(dis) " : string.Empty)}" +
                              $"+ {attackModifier:+#;-#;0} = {roll.Total} (base {roll.BaseRoll})" +
                              $"{(roll.IsCriticalSuccess ? " [CRIT]" : roll.IsCriticalFailure ? " [FUMBLE]" : string.Empty)}");

            bool hit = !roll.IsCriticalFailure; // Placeholder: no AC yet, just auto-hit unless nat 1
            if (!hit)
            {
                Console.WriteLine("  The attack misses!");
                PrintHp(defender);
                return;
            }

            int damage = context.RollService.RollDiceExpression(weapon.DamageDice, $"Damage: {weapon.DamageDice}");
            if (roll.IsCriticalSuccess)
            {
                int extra = context.RollService.RollDiceExpression(weapon.DamageDice, $"Crit extra: {weapon.DamageDice}");
                damage += extra;
            }

            defender.ApplyDamage(damage, weapon.DamageType, context);

            Console.WriteLine($"  Hit for {damage} {weapon.DamageType} damage.");
            PrintHp(defender);
        }

        private static void PrintHp(ICombatant combatant)
        {
            var hp = combatant.Resources.FirstOrDefault(r => r.Type == ResourceType.HitPoints && r.Key == "HP");
            if (hp == null)
            {
                Console.WriteLine($"  {combatant.Name}: HP unknown");
            }
            else
            {
                Console.WriteLine($"  {combatant.Name}: {hp.Current}/{hp.Maximum} HP");
            }
        }

        private static bool IsDown(ICombatant combatant)
        {
            var hp = combatant.Resources.FirstOrDefault(r => r.Type == ResourceType.HitPoints && r.Key == "HP");
            return hp != null && hp.Current <= 0;
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return string.IsNullOrWhiteSpace(name) ? "Character" : name;
        }
    }
}
