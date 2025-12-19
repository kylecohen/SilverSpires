using System;

namespace SilverSpires.Tactics.Combat
{
    public sealed class DashAction : CombatAction
    {
        public DashAction(string source)
            : base("std_dash", "Dash", source, ActionEconomySlot.Action)
        {
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

            int maxMoveTiles = creature.Stats.SpeedTiles * 2;

            Console.WriteLine($"{creature.Stats.Name} [{(actor.FactionName ?? actor.FactionId.ToString())}] takes the Dash action.");

            while (maxMoveTiles > 0)
            {
                var next = BattleHelpers.StepTowards(context.Map, creature.Position, enemy.Creature.Position);
                if (next == creature.Position) break;

                creature.MoveTo(next);
                maxMoveTiles--;
            }
        }
    }

    public sealed class DodgeAction : CombatAction
    {
        public DodgeAction(string source)
            : base("std_dodge", "Dodge", source, ActionEconomySlot.Action)
        {
        }

        public override bool CanExecute(BattleContext context, BattleUnit actor)
        {
            return actor.IsAlive;
        }

        public override void Execute(BattleContext context, BattleUnit actor)
        {
            actor.IsDodging = true;
            actor.DodgeExpiresAfterRound = context.Round;
            Console.WriteLine($"{actor.Creature.Stats.Name} [{(actor.FactionName ?? actor.FactionId.ToString())}] takes the Dodge action.");
        }
    }
}
