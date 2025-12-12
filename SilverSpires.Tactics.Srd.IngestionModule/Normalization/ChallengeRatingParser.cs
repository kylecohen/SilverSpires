using SilverSpires.Tactics.Srd.Rules;

namespace SilverSpires.Tactics.Srd.Ingestion.Normalization;

public static class ChallengeRatingParser
{
    public static ChallengeRating? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();

        if (value.Contains('/'))
        {
            var parts = value.Split('/');
            if (parts.Length != 2) return null;
            if (!int.TryParse(parts[0], out var n)) return null;
            if (!int.TryParse(parts[1], out var d) || d == 0) return null;
            return new ChallengeRating(n, d);
        }

        if (int.TryParse(value, out var whole))
            return new ChallengeRating(whole, 1);

        return null;
    }
}
