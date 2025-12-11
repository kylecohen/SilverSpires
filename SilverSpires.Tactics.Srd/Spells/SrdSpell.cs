using System.Collections.Generic;
using SilverSpires.Tactics.Srd.Rules;

namespace SilverSpires.Tactics.Srd.Spells
{
    public sealed class SrdSpellDamageComponent
    {
        public string Dice { get; set; } = "1d6";
        public DamageType DamageType { get; set; } = DamageType.Force;
    }

    public sealed class SrdSpell
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public int Level { get; set; }
        public string School { get; set; } = string.Empty;

        public string ActionType { get; set; } = string.Empty;
        public string RangeType { get; set; } = string.Empty;
        public int RangeFeet { get; set; }

        public string TargetShape { get; set; } = string.Empty;
        public int AreaRadiusFeet { get; set; }
        public int AreaLengthFeet { get; set; }
        public int AreaWidthFeet { get; set; }

        public List<string> Components { get; set; } = new();
        public bool IsRitual { get; set; }
        public bool RequiresConcentration { get; set; }

        public string Duration { get; set; } = string.Empty;
        public string CastingTimeDescription { get; set; } = string.Empty;

        public AbilityScoreType? SaveAbility { get; set; }
        public bool HalfOnSave { get; set; }

        public List<SrdSpellDamageComponent> Damage { get; set; } = new();

        public string RulesText { get; set; } = string.Empty;
        public List<string> ClassListIds { get; set; } = new();
        public List<string> Tags { get; set; } = new();
    }
}
