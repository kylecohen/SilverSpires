using System;
using System.Collections.Generic;
using System.Linq;
using SilverSpires.Tactics.Combat;
using SilverSpires.Tactics.Creatures;
using SilverSpires.Tactics.Maps;
using SilverSpires.Tactics.Srd.Data;

namespace SilverSpires.Tactics.Encounters
{
    public sealed class EncounterService
    {
        private readonly ISrdCatalog _srd;
        private readonly Random _rng;

        public EncounterService(ISrdCatalog srd, Random? rng = null)
        {
            _srd = srd ?? throw new ArgumentNullException(nameof(srd));
            _rng = rng ?? new Random();
        }

        public IReadOnlyCollection<BattleUnit> SpawnEncounter(
            GameMap map,
            EncounterDefinition definition,
            Faction faction)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            var result = new List<BattleUnit>();

            foreach (var spawn in definition.Spawns)
            {
                var monsterTemplate = _srd.GetMonsterById(spawn.MonsterId);
                if (monsterTemplate == null)
                {
                    throw new InvalidOperationException(
                        $"SRD monster not found: {spawn.MonsterId}");
                }

                var stats = new CreatureStats(monsterTemplate);

                var candidateTiles = spawn.SpawnArea
                    .EnumeratePositions()
                    .Where(p => map.IsInBounds(p) && map[p.X, p.Y].Walkable && !map[p.X, p.Y].BlocksMovement)
                    .ToList();

                if (candidateTiles.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"No valid tiles to spawn {spawn.MonsterId} in encounter {definition.Id}.");
                }

                for (int i = 0; i < spawn.Count; i++)
                {
                    var tile = candidateTiles[_rng.Next(candidateTiles.Count)];
                    var creature = new CreatureInstance(stats, tile);
                    var unit = new BattleUnit(creature, faction);

                    var parsedActions = SrdMonsterAttackParser.CreateActionsFromMonster(monsterTemplate);

                    foreach (var a in parsedActions)
                    {
                        unit.Actions.Add(a);
                    }

                    unit.Actions.Add(new DashAction(source: "Standard:Rules"));
                    unit.Actions.Add(new DodgeAction(source: "Standard:Rules"));

                    result.Add(unit);
                }
            }

            return result;
        }
    }
}
