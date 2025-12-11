using System.Collections.Generic;
using SilverSpires.Tactics.Srd.Characters;
using SilverSpires.Tactics.Srd.Items;
using SilverSpires.Tactics.Srd.Monsters;
using SilverSpires.Tactics.Srd.Rules;
using SilverSpires.Tactics.Srd.Spells;

namespace SilverSpires.Tactics.Srd.Data
{
    public interface ISrdCatalog
    {
        IReadOnlyList<SrdClass> Classes { get; }
        IReadOnlyList<SrdRace> Races { get; }
        IReadOnlyList<SrdBackground> Backgrounds { get; }
        IReadOnlyList<SrdFeat> Feats { get; }
        IReadOnlyList<SrdSkill> Skills { get; }
        IReadOnlyList<SrdLanguage> Languages { get; }
        IReadOnlyList<SrdSpell> Spells { get; }
        IReadOnlyList<SrdMonster> Monsters { get; }
        IReadOnlyList<SrdMagicItem> MagicItems { get; }
        IReadOnlyList<SrdEquipment> Equipment { get; }
        IReadOnlyList<SrdWeapon> Weapons { get; }
        IReadOnlyList<SrdArmor> Armor { get; }
        IReadOnlyList<GameEffect> Effects { get; }

        SrdMonster? GetMonsterById(string id);
        SrdWeapon? GetWeaponById(string id);
        SrdArmor? GetArmorById(string id);
    }
}
