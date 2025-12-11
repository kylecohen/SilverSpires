using System.Collections.Generic;
using SilverSpires.Tactics.Srd.Rules;

namespace SilverSpires.Tactics.Srd.Characters
{
    public sealed class SrdClass
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int HitDie { get; set; }

        public List<AbilityScoreType> PrimaryAbilities { get; set; } = new();
        public List<AbilityScoreType> SavingThrowProficiencies { get; set; } = new();

        public List<string> ArmorProficiencies { get; set; } = new();
        public List<string> WeaponProficiencies { get; set; } = new();
        public List<string> ToolProficiencies { get; set; } = new();

        public List<string> Tags { get; set; } = new();
    }

    public sealed class SrdRace
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Size { get; set; } = "Medium";
        public int BaseSpeedFeet { get; set; }
    }

    public sealed class SrdBackground
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string[] SkillProficiencies { get; set; } = System.Array.Empty<string>();
        public string[] StartingEquipmentIds { get; set; } = System.Array.Empty<string>();
        public string[] FeatureEffectIds { get; set; } = System.Array.Empty<string>();
        public string[] Tags { get; set; } = System.Array.Empty<string>();
    }

    public sealed class SrdFeat
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Prerequisites { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string[] EffectIds { get; set; } = System.Array.Empty<string>();
        public string[] Tags { get; set; } = System.Array.Empty<string>();
    }
}
