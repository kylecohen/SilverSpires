using System.Collections.Generic;
using SilverSpires.Tactics.Maps;

namespace SilverSpires.Tactics.Encounters
{
    public readonly struct RectangleArea
    {
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }

        public RectangleArea(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public IEnumerable<GridPosition> EnumeratePositions()
        {
            for (int xx = X; xx < X + Width; xx++)
            for (int yy = Y; yy < Y + Height; yy++)
                yield return new GridPosition(xx, yy);
        }
    }

    public sealed class EncounterSpawnSpec
    {
        public string MonsterId { get; set; } = string.Empty;
        public int Count { get; set; } = 1;
        public RectangleArea SpawnArea { get; set; }
        public string GroupTag { get; set; } = string.Empty;
    }

    public sealed class EncounterDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public IReadOnlyCollection<EncounterSpawnSpec> Spawns { get; set; }
            = new List<EncounterSpawnSpec>();

        public string Notes { get; set; } = string.Empty;

        public static EncounterDefinition Create(string id, string name, params EncounterSpawnSpec[] spawns)
        {
            return new EncounterDefinition
            {
                Id = id,
                Name = name,
                Spawns = spawns
            };
        }
    }
}
