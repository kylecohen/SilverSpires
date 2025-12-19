using SilverSpires.Tactics.Combat;
using SilverSpires.Tactics.Encounters;
using SilverSpires.Tactics.Srd.Persistence.Catalog;
using SilverSpires.Tactics.Srd.Persistence.Storage;

namespace SilverSpires.Tactics.Game;

/// <summary>
/// Runs a campaign in a simple terminal loop (CLI) using SRD data + game DB selections.
/// Unity will eventually have its own UI loop, but this is a production-structured runner you can reuse.
/// </summary>
public sealed class CampaignRunner
{
    private readonly IGameRepository _game;
    private readonly ISrdRepository _srdRepo;

    public CampaignRunner(IGameRepository game, ISrdRepository srdRepo)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _srdRepo = srdRepo ?? throw new ArgumentNullException(nameof(srdRepo));
    }

    public async Task RunAsync(Guid campaignId, TextReader input, TextWriter output, CancellationToken ct = default)
    {
        await _game.InitializeAsync(ct);
        await _srdRepo.InitializeAsync(ct);

        var campaign = await _game.GetCampaignAsync(campaignId, ct);
        if (campaign is null)
        {
            await output.WriteLineAsync("Campaign not found.");
            return;
        }

        var srd = new DbSrdCatalog(_srdRepo);
        await srd.LoadAsync(ct);

        var encounterService = new EncounterService(srd, rng: new Random());
        var battleRunner = new BattleRunner();

        while (!ct.IsCancellationRequested)
        {
            await output.WriteLineAsync("");
            await output.WriteLineAsync($"=== Campaign: {campaign.Name} ===");
            await output.WriteLineAsync("1) List characters");
            await output.WriteLineAsync("2) List encounters");
            await output.WriteLineAsync("3) Run encounter");
            await output.WriteLineAsync("4) Quit");
            await output.WriteAsync("> ");

            var choice = (await input.ReadLineAsync())?.Trim();

            if (choice == "4" || string.Equals(choice, "q", StringComparison.OrdinalIgnoreCase))
                return;

            if (choice == "1")
            {
                var chars = await _game.GetCampaignCharactersAsync(campaignId, ct);
                if (chars.Count == 0) { await output.WriteLineAsync("(none)"); continue; }
                foreach (var c in chars)
                    await output.WriteLineAsync($"- {c.Id}  {c.Name}  lvl {c.Level}  class={c.ClassId ?? "-"} race={c.RaceId ?? "-"}");
                continue;
            }

            if (choice == "2")
            {
                var encs = await _game.GetCampaignEncountersAsync(campaignId, ct);
                if (encs.Count == 0) { await output.WriteLineAsync("(none)"); continue; }
                foreach (var e in encs)
                    await output.WriteLineAsync($"- {e.Id}  {e.Name}");
                continue;
            }

            if (choice == "3")
            {
                var encs = await _game.GetCampaignEncountersAsync(campaignId, ct);
                if (encs.Count == 0)
                {
                    await output.WriteLineAsync("No encounters linked to this campaign.");
                    continue;
                }

                await output.WriteLineAsync("Select encounter number:");
                for (int i = 0; i < encs.Count; i++)
                    await output.WriteLineAsync($"{i + 1}) {encs[i].Name}");
                await output.WriteAsync("> ");
                var s = (await input.ReadLineAsync())?.Trim();
                if (!int.TryParse(s, out var idx) || idx < 1 || idx > encs.Count)
                {
                    await output.WriteLineAsync("Invalid selection.");
                    continue;
                }

                var enc = encs[idx - 1];
                var monsterSpecs = await _game.GetEncounterMonstersAsync(enc.Id, ct);
                var party = await _game.GetCampaignCharactersAsync(campaignId, ct);

                if (monsterSpecs.Count == 0)
                {
                    await output.WriteLineAsync("Encounter has no monsters configured.");
                    continue;
                }

                if (party.Count == 0)
                {
                    await output.WriteLineAsync("Campaign has no characters configured.");
                    continue;
                }

                // Build an EncounterDefinition from stored monster specs with a default map area.
                var spawnArea = new RectangleArea(1, 1, 8, 8);
                var spawns = monsterSpecs.Select(ms => new EncounterSpawnSpec
                {
                    MonsterId = ms.MonsterId,
                    Count = ms.Count,
                    SpawnArea = spawnArea
                }).ToArray();

                var def = EncounterDefinition.Create(enc.Id.ToString(), enc.Name, spawns);

                // Create a default map and spawn enemy units.
                var map = new SilverSpires.Tactics.Maps.GameMap(20, 20);
                var units = new List<SilverSpires.Tactics.Combat.BattleUnit>();

                units.AddRange(encounterService.SpawnEncounter(map, def, SilverSpires.Tactics.Combat.Faction.Enemy));

                // Spawn player units from stored character records.
                // NOTE: positions are basic for now; later you'll drive this from a placement phase/UI.
                var startX = 1;
                var startY = 15;
                foreach (var (c, i) in party.Select((c, i) => (c, i)))
                {
                    var tpl = new SilverSpires.Tactics.Characters.PlayerCharacterTemplate
                    {
                        Name = c.Name,
                        Level = c.Level,
                        Strength = c.Strength,
                        Dexterity = c.Dexterity,
                        Constitution = c.Constitution,
                        Intelligence = c.Intelligence,
                        Wisdom = c.Wisdom,
                        Charisma = c.Charisma,
                        ArmorId = string.IsNullOrWhiteSpace(c.ArmorId) ? "srd_chain_mail" : c.ArmorId,
                        WeaponId = string.IsNullOrWhiteSpace(c.WeaponId) ? "srd_longsword" : c.WeaponId,
                    };

                    var pos = new SilverSpires.Tactics.Maps.GridPosition(startX + (i % 6), startY + (i / 6));
                    units.Add(SilverSpires.Tactics.Characters.PlayerCharacterFactory.CreateBattleUnit(
                        srd, tpl, pos, SilverSpires.Tactics.Combat.Faction.Player));
                }

                await output.WriteLineAsync($"Starting encounter: {enc.Name}");
                await output.WriteLineAsync($"Monsters: {monsterSpecs.Sum(x => x.Count)}  Party: {party.Count}");
                await output.WriteLineAsync("");

                var winner = battleRunner.RunBattle(map, units);
                await output.WriteLineAsync($"Winner: {winner}");
await output.WriteLineAsync("Encounter complete.");
                continue;
            }

            await output.WriteLineAsync("Unknown option.");
        }
    }
}
