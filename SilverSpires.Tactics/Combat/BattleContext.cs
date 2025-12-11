using System;
using System.Collections.Generic;
using SilverSpires.Tactics.Maps;

namespace SilverSpires.Tactics.Combat
{
    public sealed class BattleContext
    {
        public GameMap Map { get; }
        public IList<BattleUnit> Units { get; }
        public Random Rng { get; }

        public int Round { get; internal set; } = 1;

        public BattleContext(GameMap map, IList<BattleUnit> units, Random? rng = null)
        {
            Map = map;
            Units = units;
            Rng = rng ?? new Random();
        }
    }
}
