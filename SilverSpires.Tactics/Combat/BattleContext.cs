using System;
using System.Collections.Generic;
using SilverSpires.Tactics.Factions;
using SilverSpires.Tactics.Maps;

namespace SilverSpires.Tactics.Combat
{
    public sealed class BattleContext
    {
        public GameMap Map { get; }
        public IList<BattleUnit> Units { get; }
        public Random Rng { get; }
        public IHostilityResolver Hostility { get; }

        public int Round { get; internal set; } = 1;

        public BattleContext(GameMap map, IList<BattleUnit> units, Random rng, IHostilityResolver hostility)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            Units = units ?? throw new ArgumentNullException(nameof(units));
            Rng = rng ?? throw new ArgumentNullException(nameof(rng));
            Hostility = hostility ?? throw new ArgumentNullException(nameof(hostility));
        }
    }
}
