using System.Text.RegularExpressions;

namespace SilverSpires.Tactics.Srd.Ingestion.Normalization;

/// <summary>
/// Parses dice strings like: "1d6+2", "2d8", "1d4 + 1".
/// Also supports "1d4+1" as bonus, but does not support complex expressions.
/// </summary>
public static class DiceParser
{
    private static readonly Regex Rx = new(
        @"^(?<count>\d+)d(?<size>\d+)(?<bonus>(?:\s*[+\-]\s*\d+)?)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool TryParse(string? dice, out int count, out int size, out int bonus)
    {
        count = 0; size = 0; bonus = 0;

        if (string.IsNullOrWhiteSpace(dice)) return false;
        var s = dice.Replace(" ", "");

        var m = Rx.Match(s);
        if (!m.Success) return false;

        count = int.Parse(m.Groups["count"].Value);
        size = int.Parse(m.Groups["size"].Value);

        var b = m.Groups["bonus"].Value;
        if (!string.IsNullOrWhiteSpace(b))
        {
            bonus = int.Parse(b.Replace(" ", ""));
        }

        return true;
    }
}
