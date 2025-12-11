using SilverSpires.Tactics.Maps;
using SilverSpires.Tactics.Creatures;
using System;
using System.Linq;

namespace SilverSpires.Tactics.Combat
{
    public enum ActionEconomySlot
    {
        Action,
        BonusAction,
        Reaction,
        Free
    }

    public abstract class CombatAction
    {
        public string Id { get; }
        public string Name { get; }
        public string Source { get; }
        public ActionEconomySlot Slot { get; }

        protected CombatAction(string id, string name, string source, ActionEconomySlot slot)
        {
            Id = id;
            Name = name;
            Source = source;
            Slot = slot;
        }

        public abstract bool CanExecute(BattleContext context, BattleUnit actor);
        public abstract void Execute(BattleContext context, BattleUnit actor);
    }

    public static class BattleHelpers
    {
        public static int Distance(GridPosition a, GridPosition b)
            => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

        public static BattleUnit? FindNearestEnemy(BattleContext ctx, BattleUnit actor)
        {
            return ctx.Units
                .Where(u => u.IsAlive && u.Faction != actor.Faction)
                .OrderBy(u => Distance(actor.Creature.Position, u.Creature.Position))
                .FirstOrDefault();
        }

        public static bool IsWalkable(GameMap map, GridPosition pos)
        {
            if (!map.IsInBounds(pos)) return false;
            var cell = map[pos.X, pos.Y];
            return cell.Walkable && !cell.BlocksMovement;
        }

        public static GridPosition StepTowards(GameMap map, GridPosition from, GridPosition target)
        {
            var bestPos = from;
            var bestDist = Distance(from, target);

            var candidates = new[]
            {
                new GridPosition(from.X + 1, from.Y),
                new GridPosition(from.X - 1, from.Y),
                new GridPosition(from.X, from.Y + 1),
                new GridPosition(from.X, from.Y - 1)
            };

            foreach (var c in candidates)
            {
                if (!IsWalkable(map, c)) continue;
                var dist = Distance(c, target);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestPos = c;
                }
            }

            return bestPos;
        }
    }
}
