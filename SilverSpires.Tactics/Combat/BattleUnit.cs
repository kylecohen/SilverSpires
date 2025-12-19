using SilverSpires.Tactics.Creatures;
using System.Collections.Generic;

namespace SilverSpires.Tactics.Combat
{
    public sealed class BattleUnit
    {
        public CreatureInstance Creature { get; }

        public Guid FactionId { get; }
        public string? FactionName { get; }

        public int Initiative { get; internal set; }

        public bool IsAlive => Creature.IsAlive;

        public List<CombatAction> Actions { get; } = new();

        public bool IsDodging { get; internal set; }
        public int DodgeExpiresAfterRound { get; internal set; }

        public BattleUnit(CreatureInstance creature, Guid factionId, string? factionName = null)
        {
            Creature = creature;
            FactionId = factionId;
            FactionName = factionName;
        }

        public override string ToString()
        {
            var f = !string.IsNullOrWhiteSpace(FactionName) ? FactionName : FactionId.ToString();
            return $"{Creature.Stats.Name} [{f}] (Init {Initiative})";
        }
    }
}
