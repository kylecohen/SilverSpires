namespace SilverSpires.Tactics.Factions;

public enum RelationshipBand
{
    Enemy,
    Hostile,
    Neutral,
    Friendly,
    Ally
}

public static class RelationshipBands
{
    public static int Clamp(int v) => v < -15 ? -15 : (v > 15 ? 15 : v);

    public static RelationshipBand ToBand(int v)
    {
        v = Clamp(v);
        if (v <= -9) return RelationshipBand.Enemy;
        if (v <= -3) return RelationshipBand.Hostile;
        if (v < 3) return RelationshipBand.Neutral;
        if (v < 9) return RelationshipBand.Friendly;
        return RelationshipBand.Ally;
    }
}
