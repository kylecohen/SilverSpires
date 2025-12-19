using SilverSpires.Tactics.Creatures;
using System.Collections.Generic;

namespace SilverSpires.Tactics.Combat
{
    public enum Faction
    {
        Player1,
        Player2,
        Goblins,
        Player,
        Enemy
    }

    public sealed class BattleUnit
    {
        public CreatureInstance Creature { get; }
        public Faction Faction { get; }

        public int Initiative { get; internal set; }

        public bool IsAlive => Creature.IsAlive;

        public List<CombatAction> Actions { get; } = new();

        public bool IsDodging { get; internal set; }
        public int DodgeExpiresAfterRound { get; internal set; }

        public BattleUnit(CreatureInstance creature, Faction faction)
        {
            Creature = creature;
            Faction = faction;
        }

        public override string ToString()
            => $"{Creature.Stats.Name} [{Faction}] HP {Creature.CurrentHitPoints}/{Creature.Stats.MaxHitPoints} (Init {Initiative})";
    }
}
