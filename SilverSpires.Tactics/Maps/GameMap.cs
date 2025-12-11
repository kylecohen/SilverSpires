using System;

namespace SilverSpires.Tactics.Maps
{
    public sealed class MapTile
    {
        public bool Walkable { get; set; } = true;
        public bool BlocksMovement { get; set; }
        public bool BlocksVision { get; set; }

        // You can extend this with terrain cost, elevation, etc.
    }

    public sealed class GameMap
    {
        private readonly MapTile[,] _tiles;

        public int Width { get; }
        public int Height { get; }

        public GameMap(int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            Width = width;
            Height = height;
            _tiles = new MapTile[width, height];

            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                _tiles[x, y] = new MapTile();
            }
        }

        public bool IsInBounds(GridPosition pos) => IsInBounds(pos.X, pos.Y);

        public bool IsInBounds(int x, int y)
            => x >= 0 && y >= 0 && x < Width && y < Height;

        public MapTile this[int x, int y]
        {
            get
            {
                if (!IsInBounds(x, y))
                    throw new ArgumentOutOfRangeException($"({x},{y}) outside map bounds.");

                return _tiles[x, y];
            }
        }
    }
}
