using System;
using SilverSpires.Tactics.Creatures;

namespace SilverSpires.Tactics.Combat
{
    public sealed class AttackAction : CombatAction
    {
        public int AttackBonus { get; }
        public int DamageDiceCount { get; }
        public int DamageDieSize { get; }
        public int DamageBonus { get; }
        public string DamageType { get; }
        public int ReachTiles { get; }
        public int MaxMoveTilesBeforeAttack { get; }

        public AttackAction(
            string id,
            string name,
            string source,
            int attackBonus,
            int damageDiceCount,
            int damageDieSize,
            int damageBonus,
            string damageType,
            int reachTiles,
            int maxMoveTilesBeforeAttack)
            : base(id, name, source, ActionEconomySlot.Action)
        {
            AttackBonus = attackBonus;
            DamageDiceCount = damageDiceCount;
            DamageDieSize = damageDieSize;
            DamageBonus = damageBonus;
            DamageType = damageType;
            ReachTiles = reachTiles;
            MaxMoveTilesBeforeAttack = maxMoveTilesBeforeAttack;
        }

        public override bool CanExecute(BattleContext context, BattleUnit actor)
        {
            if (!actor.IsAlive) return false;
            var enemy = BattleHelpers.FindNearestEnemy(context, actor);
            return enemy != null;
        }

        public override void Execute(BattleContext context, BattleUnit actor)
        {
            var enemy = BattleHelpers.FindNearestEnemy(context, actor);
            if (enemy == null) return;

            var creature = actor.Creature;
            var target = enemy.Creature;

            int dist = BattleHelpers.Distance(creature.Position, target.Position);
            int maxMove = MaxMoveTilesBeforeAttack;

            while (dist > ReachTiles && maxMove > 0)
            {
                var next = BattleHelpers.StepTowards(context.Map, creature.Position, target.Position);
                if (next == creature.Position)
                {
                    break;
                }

                creature.MoveTo(next);
                maxMove--;
                dist = BattleHelpers.Distance(creature.Position, target.Position);
            }

            if (dist > ReachTiles)
            {
                Console.WriteLine($"  {creature.Stats.Name} cannot get in range to attack.");
                return;
            }

            var rng = context.Rng;

            int RollD20() => rng.Next(1, 21);

            int d20;
            if (enemy.IsDodging && context.Round <= enemy.DodgeExpiresAfterRound)
            {
                int r1 = RollD20();
                int r2 = RollD20();
                d20 = Math.Min(r1, r2);
                Console.WriteLine($"  (Disadvantage vs dodging target: rolls {r1} and {r2}, using {d20})");
            }
            else
            {
                d20 = RollD20();
            }

            int totalAttack = d20 + AttackBonus;

            Console.WriteLine(
                $"{creature.Stats.Name} [{actor.Faction}] uses {Name} on {target.Stats.Name} [{enemy.Faction}] - roll {d20} + {AttackBonus} = {totalAttack} vs AC {target.Stats.ArmorClass}");

            if (totalAttack >= target.Stats.ArmorClass)
            {
                int damage = 0;
                for (int i = 0; i < DamageDiceCount; i++)
                {
                    damage += rng.Next(1, DamageDieSize + 1);
                }
                damage += DamageBonus;
                if (damage < 1) damage = 1;

                target.ApplyDamage(damage);
                Console.WriteLine($"  Hit! {damage} {DamageType} damage. {target.Stats.Name} HP now {target.CurrentHitPoints}/{target.Stats.MaxHitPoints}");

                if (!target.IsAlive)
                {
                    Console.WriteLine($"  {target.Stats.Name} is defeated!");
                }
            }
            else
            {
                Console.WriteLine("  Miss!");
            }
        }
    }
}
