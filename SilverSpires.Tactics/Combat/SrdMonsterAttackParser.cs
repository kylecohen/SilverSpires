using System.Collections.Generic;
using System.Text.RegularExpressions;
using SilverSpires.Tactics.Srd.Monsters;

namespace SilverSpires.Tactics.Combat
{
    public static class SrdMonsterAttackParser
    {
        private static readonly Regex AttackRegex = new Regex(
            @"\*(?<name>[^*]+)\.\*\s*(?<mode>Melee|Ranged|Melee or Ranged) Weapon Attack:\s*\+(?<atk>\d+) to hit, (?:(?:reach (?<reach>\d+) ft\.)|(?:range (?<r1>\d+)\/(?<r2>\d+) ft\.)), one target\. Hit: (?<avg>\d+) \((?<diceCount>\d+)d(?<dieSize>\d+)(?: \+ (?<dmgBonus>\d+))?\) (?<dtype>\w+) damage",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static IReadOnlyList<AttackAction> CreateActionsFromMonster(SrdMonster monster)
        {
            var result = new List<AttackAction>();

            foreach (Match match in AttackRegex.Matches(monster.ActionsMarkdown))
            {
                var name = match.Groups["name"].Value.Trim();
                var atkBonus = int.Parse(match.Groups["atk"].Value);
                var diceCount = int.Parse(match.Groups["diceCount"].Value);
                var dieSize = int.Parse(match.Groups["dieSize"].Value);
                var dmgBonusGroup = match.Groups["dmgBonus"];
                int dmgBonus = dmgBonusGroup.Success ? int.Parse(dmgBonusGroup.Value) : 0;
                var dtype = match.Groups["dtype"].Value.Trim().ToLowerInvariant();

                int reachFeet = 5;
                if (match.Groups["reach"].Success)
                {
                    reachFeet = int.Parse(match.Groups["reach"].Value);
                }

                int reachTiles = reachFeet / 5;
                if (reachTiles < 1) reachTiles = 1;

                var action = new AttackAction(
                    id: $"monster_attack:{monster.Id}:{name.Replace(' ', '_')}",
                    name: name,
                    source: $"Monster:{monster.Id}",
                    attackBonus: atkBonus,
                    damageDiceCount: diceCount,
                    damageDieSize: dieSize,
                    damageBonus: dmgBonus,
                    damageType: dtype,
                    reachTiles: reachTiles,
                    maxMoveTilesBeforeAttack: monster.SpeedWalk / 5);

                result.Add(action);
            }

            return result;
        }
    }
}
