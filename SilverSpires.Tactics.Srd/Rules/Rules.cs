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
        public double Numeric { get; }
        public string? Text { get; }

        private ChallengeRating(double numeric, string? text)
        {
            Numeric = numeric;
            Text = text;
        }

        public static ChallengeRating FromNumeric(double numeric) => new ChallengeRating(numeric, null);
        public static ChallengeRating FromText(string text) => new ChallengeRating(0, text);

        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(Text)) return Text!;
            // Pretty print common fractions
            if (Math.Abs(Numeric - 0.125) < 0.0001) return "1/8";
            if (Math.Abs(Numeric - 0.25) < 0.0001) return "1/4";
            if (Math.Abs(Numeric - 0.5) < 0.0001) return "1/2";
            if (Math.Abs(Numeric - Math.Round(Numeric)) < 0.0001) return ((int)Math.Round(Numeric)).ToString();
            return Numeric.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
