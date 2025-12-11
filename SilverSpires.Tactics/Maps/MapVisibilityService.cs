using System;
using System.Collections.Generic;

namespace SilverSpires.Tactics.Maps
{
    /// <summary>
    /// Very simple visibility / fog-of-war helper that does diamond-shaped range checks.
    /// You can replace this with proper raycasting later.
    /// </summary>
    public static class MapVisibilityService
    {
        public static HashSet<GridPosition> ComputeVisibleTiles(GameMap map, GridPosition origin, int range)
        {
            var result = new HashSet<GridPosition>();

            for (int dx = -range; dx <= range; dx++)
            {
                for (int dy = -range; dy <= range; dy++)
                {
                    var pos = new GridPosition(origin.X + dx, origin.Y + dy);
                    if (!map.IsInBounds(pos)) continue;

                    // Simple manhattan distance
                    int dist = Math.Abs(dx) + Math.Abs(dy);
                    if (dist <= range)
                    {
                        result.Add(pos);
                    }
                }
            }

            return result;
        }
    }
}
