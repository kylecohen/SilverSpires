using System;

namespace SilverSpires.Tactics.Maps
{
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public int X { get; }
        public int Y { get; }

        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(GridPosition other) => X == other.X && Y == other.Y;
        public override bool Equals(object? obj) => obj is GridPosition other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);

        public static bool operator ==(GridPosition left, GridPosition right) => left.Equals(right);
        public static bool operator !=(GridPosition left, GridPosition right) => !left.Equals(right);

        public override string ToString() => $"({X},{Y})";
    }
}
