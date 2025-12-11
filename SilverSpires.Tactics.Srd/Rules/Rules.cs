namespace SilverSpires.Tactics.Srd.Rules
{
    public enum AbilityScoreType
    {
        Strength,
        Dexterity,
        Constitution,
        Intelligence,
        Wisdom,
        Charisma
    }

    public enum SizeCategory
    {
        Tiny,
        Small,
        Medium,
        Large,
        Huge,
        Gargantuan
    }

    public enum CreatureType
    {
        Humanoid,
        Beast,
        Fiend,
        Undead,
        Dragon,
        Aberration,
        Construct,
        Elemental,
        Fey,
        Giant,
        Monstrosity,
        Ooze,
        Plant,
        Celestial,
        Other
    }

    public enum DamageType
    {
        Slashing,
        Piercing,
        Bludgeoning,
        Fire,
        Cold,
        Lightning,
        Force,
        Poison,
        Acid,
        Psychic,
        Radiant,
        Necrotic,
        Thunder
    }

    public readonly struct ChallengeRating
    {
        public int Numerator { get; }
        public int Denominator { get; }

        public ChallengeRating(int numerator, int denominator)
        {
            Numerator = numerator;
            Denominator = denominator == 0 ? 1 : denominator;
        }

        public double ToDouble() => (double)Numerator / Denominator;

        public override string ToString()
        {
            if (Denominator == 1) return Numerator.ToString();
            return $"{Numerator}/{Denominator}";
        }
    }
}
