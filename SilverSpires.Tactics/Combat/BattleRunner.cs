using System;
using System.Collections.Generic;
using System.Linq;
using SilverSpires.Tactics.Creatures;
using SilverSpires.Tactics.Maps;

namespace SilverSpires.Tactics.Combat
{
    public sealed class BattleRunner
    {
        public Faction RunBattle(GameMap map, IList<BattleUnit> units, Random? rng = null)
        {
            var rand = rng ?? new Random();
            var ctx = new BattleContext(map, units, rand);

            foreach (var u in units)
            {
                int dexMod = CreatureStats.AbilityMod(u.Creature.Stats.Dexterity);
                int initRoll = rand.Next(1, 21) + dexMod;
                u.Initiative = initRoll;
            }

            var turnOrder = units
                .OrderByDescending(u => u.Initiative)
                .ToList();

            Console.WriteLine("== Battle Start ==");
            foreach (var u in turnOrder)
            {
                Console.WriteLine($"  {u}");
            }

            int index = 0;

            while (true)
            {
                var aliveFactions = turnOrder
                    .Where(u => u.IsAlive)
                    .Select(u => u.Faction)
                    .Distinct()
                    .ToList();

                if (aliveFactions.Count <= 1)
                {
                    var winner = aliveFactions.Single();
                    Console.WriteLine($"== Battle Over: {winner} wins ==");
                    return winner;
                }

                if (index == 0)
                {
                    Console.WriteLine();
                    Console.WriteLine($"== Round {ctx.Round} ==");
                }

                var actor = turnOrder[index];
                index = (index + 1) % turnOrder.Count;

                if (!actor.IsAlive)
                {
                    if (index == 0) ctx.Round++;
                    continue;
                }

                if (actor.IsDodging && actor.DodgeExpiresAfterRound < ctx.Round)
                {
                    actor.IsDodging = false;
                }

                Console.WriteLine();
                Console.WriteLine($"-- {actor.Creature.Stats.Name} [{actor.Faction}]'s turn (Init {actor.Initiative}) --");

                var attackActions = actor.Actions.OfType<AttackAction>().ToList();
                var dash = actor.Actions.OfType<DashAction>().FirstOrDefault();
                var dodge = actor.Actions.OfType<DodgeAction>().FirstOrDefault();

                bool acted = false;

                foreach (var action in attackActions)
                {
                    if (action.CanExecute(ctx, actor))
                    {
                        action.Execute(ctx, actor);
                        acted = true;
                        break;
                    }
                }

                if (!acted && dash != null && dash.CanExecute(ctx, actor))
                {
                    dash.Execute(ctx, actor);
                    acted = true;
                }

                if (!acted && dodge != null && dodge.CanExecute(ctx, actor))
                {
                    dodge.Execute(ctx, actor);
                    acted = true;
                }

                if (!acted)
                {
                    Console.WriteLine("  No valid action.");
                }

                if (index == 0)
                {
                    ctx.Round++;
                }
            }
        }
    }
}
