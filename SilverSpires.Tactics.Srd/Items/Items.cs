using System.Collections.Generic;
using SilverSpires.Tactics.Srd.Rules;

namespace SilverSpires.Tactics.Srd.Items
{
    public class SrdEquipment
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int CostCopper { get; set; }
        public double WeightPounds { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }

    public class SrdWeapon : SrdEquipment
    {
        public string WeaponCategory { get; set; } = string.Empty;
        public string DamageDice { get; set; } = "1d6";
        public DamageType DamageType { get; set; } = DamageType.Slashing;
        public int RangeNormalFeet { get; set; }
        public int RangeMaxFeet { get; set; }
        public List<string> Properties { get; set; } = new();
    }

    public class SrdArmor : SrdEquipment
    {
        public string ArmorCategory { get; set; } = string.Empty;
        public int ArmorClassBase { get; set; }
        public bool AddsDexterityModifier { get; set; }
        public int? MaxDexterityBonus { get; set; }
        public int StrengthRequirement { get; set; }
        public bool ImposesStealthDisadvantage { get; set; }
    }

    public class SrdMagicItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Rarity { get; set; } = string.Empty;
        public bool RequiresAttunement { get; set; }
        public string AttunementRequirement { get; set; } = string.Empty;
        public List<string> EffectIds { get; set; } = new();
        public string RulesText { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }
}
