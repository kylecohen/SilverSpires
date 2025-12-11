using System;
using SilverSpires.Tactics.Combat;
using SilverSpires.Tactics.Creatures;
using SilverSpires.Tactics.Maps;
using SilverSpires.Tactics.Srd.Data;
using SilverSpires.Tactics.Srd.Items;
using SilverSpires.Tactics.Srd.Rules;

namespace SilverSpires.Tactics.Characters
{
    public sealed class PlayerCharacterTemplate
    {
        public string Name { get; init; } = string.Empty;
        public int Level { get; init; } = 1;

        public int Strength { get; init; }
        public int Dexterity { get; init; }
        public int Constitution { get; init; }
        public int Intelligence { get; init; }
        public int Wisdom { get; init; }
        public int Charisma { get; init; }

        public string ArmorId { get; init; } = "srd_chain_mail";
        public string WeaponId { get; init; } = "srd_longsword";
    }

    public static class PlayerCharacterFactory
    {
        public static BattleUnit CreateBattleUnit(
            ISrdCatalog srd,
            PlayerCharacterTemplate tpl,
            GridPosition startPosition,
            Faction faction)
        {
            var armor = srd.GetArmorById(tpl.ArmorId)
                        ?? throw new InvalidOperationException($"Armor not found: {tpl.ArmorId}");
            var weapon = srd.GetWeaponById(tpl.WeaponId)
                         ?? throw new InvalidOperationException($"Weapon not found: {tpl.WeaponId}");

            int strMod = CreatureStats.AbilityMod(tpl.Strength);
            int dexMod = CreatureStats.AbilityMod(tpl.Dexterity);
            int conMod = CreatureStats.AbilityMod(tpl.Constitution);

            int maxHp = 10 + conMod;

            int armorClass = armor.ArmorClassBase;
            if (armor.AddsDexterityModifier)
            {
                int dexToAdd = dexMod;
                if (armor.MaxDexterityBonus.HasValue)
                    dexToAdd = Math.Min(dexToAdd, armor.MaxDexterityBonus.Value);

                armorClass += dexToAdd;
            }

            int speedFeet = 30;

            var stats = new CreatureStats(
                templateId: $"pc_{tpl.Name.Replace(' ', '_')}",
                name: tpl.Name,
                size: SizeCategory.Medium,
                creatureType: CreatureType.Humanoid,
                armorClass: armorClass,
                maxHitPoints: maxHp,
                strength: tpl.Strength,
                dexterity: tpl.Dexterity,
                constitution: tpl.Constitution,
                intelligence: tpl.Intelligence,
                wisdom: tpl.Wisdom,
                charisma: tpl.Charisma,
                speedFeet: speedFeet,
                challengeRating: new ChallengeRating(0, 1));

            var creature = new CreatureInstance(stats, startPosition);
            var unit = new BattleUnit(creature, faction);

            int proficiency = 2;
            int attackBonus = proficiency + strMod;

            var parts = weapon.DamageDice.Split('d');
            int diceCount = int.Parse(parts[0]);
            int dieSize = int.Parse(parts[1]);

            var attack = new AttackAction(
                id: $"weapon_attack:{weapon.Id}",
                name: $"{tpl.Name}'s {weapon.Name}",
                source: $"Weapon:{weapon.Id}",
                attackBonus: attackBonus,
                damageDiceCount: diceCount,
                damageDieSize: dieSize,
                damageBonus: strMod,
                damageType: weapon.DamageType.ToString().ToLowerInvariant(),
                reachTiles: 1,
                maxMoveTilesBeforeAttack: stats.SpeedTiles);

            unit.Actions.Add(attack);
            unit.Actions.Add(new DashAction(source: "Standard:Rules"));
            unit.Actions.Add(new DodgeAction(source: "Standard:Rules"));

            return unit;
        }
    }
}
