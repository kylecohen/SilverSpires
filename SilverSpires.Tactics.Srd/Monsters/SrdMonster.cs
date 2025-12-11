using System.Collections.Generic;
using SilverSpires.Tactics.Srd.Rules;

namespace SilverSpires.Tactics.Srd.Monsters
{
    public sealed class SrdMonster
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public SizeCategory Size { get; set; } = SizeCategory.Medium;
        public CreatureType Type { get; set; } = CreatureType.Humanoid;
        public string Alignment { get; set; } = string.Empty;

        public int ArmorClass { get; set; }
        public string ArmorNotes { get; set; } = string.Empty;

        public int HitPointsAverage { get; set; }
        public string HitDice { get; set; } = string.Empty;

        public int SpeedWalk { get; set; }
        public int SpeedFly { get; set; }
        public int SpeedSwim { get; set; }
        public int SpeedClimb { get; set; }
        public int SpeedBurrow { get; set; }

        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Constitution { get; set; }
        public int Intelligence { get; set; }
        public int Wisdom { get; set; }
        public int Charisma { get; set; }

        public Dictionary<string, int> SavingThrows { get; set; } = new();
        public Dictionary<string, int> Skills { get; set; } = new();

        public string Senses { get; set; } = string.Empty;
        public int PassivePerception { get; set; }

        public string Languages { get; set; } = string.Empty;

        public ChallengeRating ChallengeRating { get; set; }

        public string[] DamageVulnerabilities { get; set; } = System.Array.Empty<string>();
        public string[] DamageResistances { get; set; } = System.Array.Empty<string>();
        public string[] DamageImmunities { get; set; } = System.Array.Empty<string>();
        public string[] ConditionImmunities { get; set; } = System.Array.Empty<string>();

        public string TraitsMarkdown { get; set; } = string.Empty;
        public string ActionsMarkdown { get; set; } = string.Empty;
        public string ReactionsMarkdown { get; set; } = string.Empty;
        public string LegendaryActionsMarkdown { get; set; } = string.Empty;

        public List<string> Tags { get; set; } = new();
    }
}
