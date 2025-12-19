using System;
using System.Collections.Generic;
using System.Linq;
using SilverSpires.Tactics.Maps;
using SilverSpires.Tactics.Factions;

namespace SilverSpires.Tactics.Combat
{
    public sealed class BattleRunner
    {
        public Guid? RunBattle(GameMap map, IList<BattleUnit> units, IHostilityResolver? hostility = null, Random? rng = null)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (units == null) throw new ArgumentNullException(nameof(units));

            var rand = rng ?? new Random();
            var host = hostility ?? new DefaultHostilityResolver();
            var ctx = new BattleContext(map, units, rand, host);

            foreach (var u in units)
                u.Initiative = rand.Next(1, 21) + u.Creature.Stats.InitiativeBonus;

            var turnOrder = units
                .OrderByDescending(u => u.Initiative)
                .ToList();

            Console.WriteLine("== Battle Start ==");
            foreach (var u in turnOrder)
                Console.WriteLine($"  {u}");

            int index = 0;

            while (true)
            {
                // Determine whether any hostile pairs remain alive
                var alive = turnOrder.Where(u => u.IsAlive).ToList();
                if (alive.Count == 0) return null;

                bool anyHostile = false;
                for (int i = 0; i < alive.Count && !anyHostile; i++)
                for (int j = i + 1; j < alive.Count && !anyHostile; j++)
                    if (ctx.Hostility.AreHostile(alive[i].FactionId, alive[j].FactionId))
                        anyHostile = true;

                if (!anyHostile)
                {
                    // peaceful stalemate; if exactly one faction remains, return it.
                    var remainingFactions = alive.Select(a => a.FactionId).Distinct().ToList();
                    return remainingFactions.Count == 1 ? remainingFactions[0] : null;
                }

                var actor = turnOrder[index % turnOrder.Count];
                index++;

                if (!actor.IsAlive) continue;

                var actorFactionLabel = actor.FactionName ?? actor.FactionId.ToString();
                Console.WriteLine($"-- {actor.Creature.Stats.Name} [{actorFactionLabel}]'s turn (Init {actor.Initiative}) --");

                // Choose a hostile target
                var targets = alive
                    .Where(u => u.IsAlive && ctx.Hostility.AreHostile(actor.FactionId, u.FactionId))
                    .ToList();

                if (targets.Count == 0)
                {
                    Console.WriteLine("  (no hostile targets)");
                    NextRoundIfNeeded(ctx, turnOrder, index);
                    continue;
                }

                var target = targets[rand.Next(targets.Count)];

                // Basic: attack action (existing combat actions may do more)
                var attack = actor.Actions.OfType<AttackAction>().FirstOrDefault();
                if (attack == null)
                {
                    Console.WriteLine("  (no attack action available)");
                    NextRoundIfNeeded(ctx, turnOrder, index);
                    continue;
                }

                attack.Execute(ctx, actor, target);

                NextRoundIfNeeded(ctx, turnOrder, index);
            }
        }

        private static void NextRoundIfNeeded(BattleContext ctx, List<BattleUnit> turnOrder, int index)
        {
            if (index % turnOrder.Count == 0)
            {
                ctx.Round++;
                foreach (var u in turnOrder)
                {
                    if (u.IsDodging && u.DodgeExpiresAfterRound < ctx.Round)
                        u.IsDodging = false;
                }
                Console.WriteLine($"== Round {ctx.Round} ==");
            }
        }
    }
}
