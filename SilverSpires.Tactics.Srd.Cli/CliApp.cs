using System.Text;
using System.Text.Json;
using SilverSpires.Tactics.Game;
using SilverSpires.Tactics.Srd.Persistence.Storage;
using SilverSpires.Tactics.Srd.Persistence.Storage.Json;
using SilverSpires.Tactics.Sync;

namespace SilverSpires.Tactics.Srd.Cli;

public sealed class CliApp
{
    private readonly ISrdRepository _srdRepo;
    private readonly IGameRepository _gameRepo;
    private readonly JsonSerializerOptions _json;
    private readonly HttpClient _api;
    private readonly SrdSyncClient _sync;

    public CliApp(ISrdRepository srdRepo, IGameRepository gameRepo, HttpClient api)
    {
        _srdRepo = srdRepo;
        _gameRepo = gameRepo;
        _api = api;

        _json = SrdJsonOptions.CreateDefault();
        _sync = new SrdSyncClient(_api, _srdRepo, _json);
    }

    public async Task<int> RunAsync(string[] args, CancellationToken ct = default)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return 1;
        }

        var cmd = args[0].ToLowerInvariant();
        var rest = args.Skip(1).ToArray();

        switch (cmd)
        {
            case "help":
            case "--help":
            case "-h":
                PrintHelp();
                return 0;

            // ---------------- SRD ----------------
            case "srd":
                return await RunSrdAsync(rest, ct);

            // ---------------- SERVER ----------------
            case "server":
                return await RunServerAsync(rest, ct);

            // ---------------- CAMPAIGN ----------------
            case "campaign":
                return await RunCampaignAsync(rest, ct);

            // ---------------- ENCOUNTER ----------------
            case "encounter":
                return await RunEncounterAsync(rest, ct);

            // ---------------- CHARACTER ----------------
            case "character":
                return await RunCharacterAsync(rest, ct);

            // ---------------- DEV/TEST ----------------
            case "dev":
                return await RunDevAsync(rest, ct);

            // Back-compat (existing CLI commands)
            case "bootstrap":
            case "sources":
            case "update":
                Console.WriteLine("These legacy ingestion commands are still supported, but prefer: server refresh-srd + srd sync.");
                // defer to existing legacy handler (in Program.cs)
                return 2;

            default:
                Console.WriteLine($"Unknown command: {cmd}");
                PrintHelp();
                return 1;
        }
    }

    private async Task<int> RunSrdAsync(string[] args, CancellationToken ct)
    {
        if (args.Length == 0) { PrintSrdHelp(); return 1; }

        var sub = args[0].ToLowerInvariant();
        switch (sub)
        {
            case "sync":
            {
                await _srdRepo.InitializeAsync(ct);

                // Marker is stored in game DB settings (so Unity and CLI can share it).
                var last = await _gameRepo.GetSettingAsync("srd.lastSyncUtc", ct);
                DateTime? lastUtc = null;

                // override: --since <iso>
                var since = GetArgValue(args, "--since");
                if (!string.IsNullOrWhiteSpace(since) && DateTime.TryParse(since, out var parsed))
                    lastUtc = parsed.ToUniversalTime();
                else if (!string.IsNullOrWhiteSpace(last) && DateTime.TryParse(last, out parsed))
                    lastUtc = parsed.ToUniversalTime();

                var newMarker = await _sync.SyncAllAsync(lastUtc, ct);

                if (newMarker.HasValue)
                    await _gameRepo.SetSettingAsync("srd.lastSyncUtc", newMarker.Value.ToString("o"), ct);

                Console.WriteLine($"SRD sync complete. lastSyncUtc={(newMarker?.ToString("o") ?? "(unchanged)")}");
                return 0;
            }

            case "manifest":
            {
                var server = await _sync.GetServerLatestUpdatedUtcAsync(ct);
                var local = await _srdRepo.GetLatestEntityUpdatedUtcAsync(ct);
                Console.WriteLine($"Server latest: {(server?.ToString("o") ?? "(none)")}");
                Console.WriteLine($"Local latest:  {(local?.ToString("o") ?? "(none)")}");
                return 0;
            }

            default:
                PrintSrdHelp();
                return 1;
        }
    }

    private async Task<int> RunServerAsync(string[] args, CancellationToken ct)
    {
        if (args.Length == 0) { PrintServerHelp(); return 1; }
        var sub = args[0].ToLowerInvariant();

        switch (sub)
        {
            case "refresh-srd":
            {
                var apiKey = Environment.GetEnvironmentVariable("SRD_API_KEY");
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    if (!_api.DefaultRequestHeaders.Contains("X-API-Key"))
                        _api.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                }

                var resp = await _api.PostAsync("/api/admin/refresh-srd", content: null, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                if (!resp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Refresh request failed: {(int)resp.StatusCode} {resp.ReasonPhrase}");
                    Console.WriteLine(body);
                    return 1;
                }

                Console.WriteLine("Refresh job started:");
                Console.WriteLine(body);
                return 0;
            }

            default:
                PrintServerHelp();
                return 1;
        }
    }

    private async Task<int> RunCampaignAsync(string[] args, CancellationToken ct)
    {
        await _gameRepo.InitializeAsync(ct);

        if (args.Length == 0) { PrintCampaignHelp(); return 1; }
        var sub = args[0].ToLowerInvariant();

        switch (sub)
        {
            case "list":
            {
                var list = await _gameRepo.ListCampaignsAsync(ct);
                if (list.Count == 0) { Console.WriteLine("(no campaigns)"); return 0; }
                foreach (var c in list)
                    Console.WriteLine($"{c.Id}  {c.Name}");
                return 0;
            }

            case "new":
            {
                var name = PromptRequired("Campaign name: ");
                var desc = PromptOptional("Description (optional): ");

                var c = await _gameRepo.CreateCampaignAsync(name, desc, ct);

                // Select encounters + characters
                var encIds = await SelectEncountersAsync(ct);
                var charIds = await SelectCharactersAsync(ct);

                await _gameRepo.SetCampaignEncountersAsync(c.Id, encIds, ct);
                await _gameRepo.SetCampaignCharactersAsync(c.Id, charIds, ct);

                await _gameRepo.SetSettingAsync("campaign.currentId", c.Id.ToString(), ct);

                Console.WriteLine($"Created campaign: {c.Id}  {c.Name}");
                Console.WriteLine("Set as current campaign.");
                return 0;
            }

            case "select":
            case "load":
            {
                var list = await _gameRepo.ListCampaignsAsync(ct);
                if (list.Count == 0) { Console.WriteLine("(no campaigns)"); return 0; }

                Console.WriteLine("Select campaign:");
                for (var i = 0; i < list.Count; i++)
                    Console.WriteLine($"{i + 1}) {list[i].Name} ({list[i].Id})");

                var idx = PromptInt("Enter number: ", 1, list.Count);
                var c = list[idx - 1];

                await _gameRepo.SetSettingAsync("campaign.currentId", c.Id.ToString(), ct);
                Console.WriteLine($"Current campaign set: {c.Name}");
                return 0;
            }

            case "start":
            {
                var idArg = GetArgValue(args, "--id");
                Guid id;

                if (!string.IsNullOrWhiteSpace(idArg) && Guid.TryParse(idArg, out var parsed))
                    id = parsed;
                else
                {
                    var cur = await _gameRepo.GetSettingAsync("campaign.currentId", ct);
                    if (string.IsNullOrWhiteSpace(cur) || !Guid.TryParse(cur, out id))
                    {
                        Console.WriteLine("No current campaign. Use: campaign new OR campaign load");
                        return 1;
                    }
                }

                await _srdRepo.InitializeAsync(ct);

                var runner = new CampaignRunner(_gameRepo, _srdRepo);
                await runner.RunAsync(id, Console.In, Console.Out, ct);
                return 0;
            }

            default:
                PrintCampaignHelp();
                return 1;
        }
    }

    private async Task<int> RunEncounterAsync(string[] args, CancellationToken ct)
    {
        await _gameRepo.InitializeAsync(ct);
        await _srdRepo.InitializeAsync(ct);

        if (args.Length == 0) { PrintEncounterHelp(); return 1; }
        var sub = args[0].ToLowerInvariant();

        switch (sub)
        {
            case "new":
            {
                // Ensure SRD is loaded enough to select monsters
                var catalog = await LoadSrdCatalogAsync(ct);

                var name = PromptRequired("Encounter name: ");
                var notes = PromptOptional("Notes (optional): ");

                var enc = await _gameRepo.CreateEncounterAsync(new EncounterRecord(Guid.Empty, name, notes, default, default), ct);

                // Select monsters
                var monsters = SelectMonsters(catalog.Monsters.Select(m => (m.Id, m.Name)).ToList());

                await _gameRepo.SetEncounterMonstersAsync(enc.Id,
                    monsters.Select(m => new EncounterMonsterRecord(enc.Id, m.monsterId, m.count)),
                    ct);

                // Select characters (optional)
                var chars = await _gameRepo.ListCharactersAsync(ct);
                if (chars.Count > 0 && PromptYesNo("Add characters to this encounter now? (y/n): "))
                {
                    var charIds = SelectByList("Select characters for encounter", chars.Select(c => (c.Id, c.Name)).ToList());
                    await _gameRepo.SetEncounterCharactersAsync(enc.Id, charIds, ct);
                }

                Console.WriteLine($"Created encounter: {enc.Id}  {enc.Name}");
                return 0;
            }

            case "list":
            {
                var list = await _gameRepo.ListEncountersAsync(ct);
                if (list.Count == 0) { Console.WriteLine("(no encounters)"); return 0; }
                foreach (var e in list)
                    Console.WriteLine($"{e.Id}  {e.Name}");
                return 0;
            }

            default:
                PrintEncounterHelp();
                return 1;
        }
    }

    private async Task<int> RunCharacterAsync(string[] args, CancellationToken ct)
    {
        await _gameRepo.InitializeAsync(ct);
        await _srdRepo.InitializeAsync(ct);

        if (args.Length == 0) { PrintCharacterHelp(); return 1; }
        var sub = args[0].ToLowerInvariant();

        switch (sub)
        {
            case "new":
            {
                var catalog = await LoadSrdCatalogAsync(ct);

                var name = PromptRequired("Character name: ");
                var notes = PromptOptional("Notes (optional): ");
                var level = PromptInt("Level (1-20): ", 1, 20);

                var classId = SelectOptionalSrd("Select class (optional)", catalog.Classes.Select(c => (c.Id, c.Name)).ToList());
                var raceId = SelectOptionalSrd("Select race (optional)", catalog.Races.Select(r => (r.Id, r.Name)).ToList());

                // Ability scores (production-relevant: stored explicitly)
                Console.WriteLine("Ability scores (3-20 typical):");
                var str = PromptInt("  STR: ", 1, 30);
                var dex = PromptInt("  DEX: ", 1, 30);
                var con = PromptInt("  CON: ", 1, 30);
                var intel = PromptInt("  INT: ", 1, 30);
                var wis = PromptInt("  WIS: ", 1, 30);
                var cha = PromptInt("  CHA: ", 1, 30);

                var armorId = SelectRequiredSrd("Select armor", catalog.Armor.Select(a => (a.Id, a.Name)).ToList());
                var weaponId = SelectRequiredSrd("Select weapon", catalog.Weapons.Select(w => (w.Id, w.Name)).ToList());

                var rec = new CharacterRecord(
                    Guid.Empty, name, notes, classId, raceId, level,
                    str, dex, con, intel, wis, cha,
                    armorId, weaponId,
                    default, default);

                var created = await _gameRepo.CreateCharacterAsync(rec, ct);
                Console.WriteLine($"Created character: {created.Id}  {created.Name}");
                return 0;
            }

            case "list":
            {
                var list = await _gameRepo.ListCharactersAsync(ct);
                if (list.Count == 0) { Console.WriteLine("(no characters)"); return 0; }
                foreach (var c in list)
                    Console.WriteLine($"{c.Id}  {c.Name}  lvl {c.Level}");
                return 0;
            }

            default:
                PrintCharacterHelp();
                return 1;
        }
    }

    // -----------------------
    // SRD catalog loading
    // -----------------------
    private async Task<SilverSpires.Tactics.Srd.Data.ISrdCatalog> LoadSrdCatalogAsync(CancellationToken ct)
    {
        var cat = new SilverSpires.Tactics.Srd.Persistence.Catalog.DbSrdCatalog(_srdRepo);
        await cat.LoadAsync(ct);
        return cat;
    }

    // -----------------------
    // Helpers: selection + prompts
    // -----------------------
    private static string PromptRequired(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            var s = Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(s)) return s!;
            Console.WriteLine("Required.");
        }
    }

    private static string? PromptOptional(string prompt)
    {
        Console.Write(prompt);
        var s = Console.ReadLine();
        s = s?.Trim();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    private static int PromptInt(string prompt, int min, int max)
    {
        while (true)
        {
            Console.Write(prompt);
            var s = Console.ReadLine()?.Trim();
            if (int.TryParse(s, out var v) && v >= min && v <= max) return v;
            Console.WriteLine($"Enter a number between {min} and {max}.");
        }
    }

    private static bool PromptYesNo(string prompt)
    {
        Console.Write(prompt);
        var s = Console.ReadLine()?.Trim();
        return s != null && (s.Equals("y", StringComparison.OrdinalIgnoreCase) || s.Equals("yes", StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetArgValue(string[] args, string key)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                return args[i + 1];
        }
        return null;
    }

    private async Task<List<Guid>> SelectEncountersAsync(CancellationToken ct)
    {
        var encs = await _gameRepo.ListEncountersAsync(ct);
        if (encs.Count == 0)
        {
            Console.WriteLine("No encounters yet. Create one with: encounter new");
            return new List<Guid>();
        }

        var ids = SelectByList("Select encounters for campaign", encs.Select(e => (e.Id, e.Name)).ToList());
        return ids;
    }

    private async Task<List<Guid>> SelectCharactersAsync(CancellationToken ct)
    {
        var chars = await _gameRepo.ListCharactersAsync(ct);
        if (chars.Count == 0)
        {
            Console.WriteLine("No characters yet. Create one with: character new");
            return new List<Guid>();
        }

        var ids = SelectByList("Select characters for campaign", chars.Select(c => (c.Id, c.Name)).ToList());
        return ids;
    }

    private static List<Guid> SelectByList(string title, List<(Guid id, string name)> items)
    {
        Console.WriteLine(title);
        for (int i = 0; i < items.Count; i++)
            Console.WriteLine($"{i + 1}) {items[i].name}");

        Console.WriteLine("Enter comma-separated numbers (or blank for none):");
        Console.Write("> ");
        var s = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(s)) return new List<Guid>();

        var chosen = new HashSet<Guid>();
        foreach (var tok in s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(tok, out var idx) || idx < 1 || idx > items.Count) continue;
            chosen.Add(items[idx - 1].id);
        }
        return chosen.ToList();
    }

    private static string? SelectOptionalSrd(string title, List<(string id, string name)> items)
    {
        Console.WriteLine(title);
        Console.WriteLine("0) (none)");
        for (int i = 0; i < items.Count; i++)
            Console.WriteLine($"{i + 1}) {items[i].name} ({items[i].id})");

        var idx = PromptInt("Enter number: ", 0, items.Count);
        return idx == 0 ? null : items[idx - 1].id;
    }

    private static string SelectRequiredSrd(string title, List<(string id, string name)> items)
    {
        Console.WriteLine(title);
        for (int i = 0; i < items.Count; i++)
            Console.WriteLine($"{i + 1}) {items[i].name} ({items[i].id})");

        var idx = PromptInt("Enter number: ", 1, items.Count);
        return items[idx - 1].id;
    }

    private static List<(string monsterId, int count)> SelectMonsters(List<(string id, string name)> monsters)
    {
        Console.WriteLine("Select monsters for encounter.");
        Console.WriteLine("Tip: You can search by typing a substring, then select numbers.");
        var selected = new List<(string monsterId, int count)>();

        while (true)
        {
            Console.Write("Search (blank to finish): ");
            var q = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(q)) break;

            var filtered = monsters
                .Where(m => m.name.Contains(q, StringComparison.OrdinalIgnoreCase) || m.id.Contains(q, StringComparison.OrdinalIgnoreCase))
                .Take(25)
                .ToList();

            if (filtered.Count == 0)
            {
                Console.WriteLine("(no matches)");
                continue;
            }

            for (int i = 0; i < filtered.Count; i++)
                Console.WriteLine($"{i + 1}) {filtered[i].name} ({filtered[i].id})");

            var idx = PromptInt("Pick number: ", 1, filtered.Count);
            var count = PromptInt("Count: ", 1, 100);

            selected.Add((filtered[idx - 1].id, count));
            Console.WriteLine("Added.");
        }

        return selected;
    }

    
    private async Task<int> RunDevAsync(string[] args, CancellationToken ct)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("dev commands: init | reset | seed | wipe-srd");
            return 1;
        }

        var sub = args[0].ToLowerInvariant();
        switch (sub)
        {
            case "reset":
            {
                // Delete local game db only (safe).
                var gamePath = Environment.GetEnvironmentVariable("SS_GAME_SQLITE_PATH") ?? Path.Combine(AppContext.BaseDirectory, "data", "game.sqlite");
                if (File.Exists(gamePath))
                {
                    File.Delete(gamePath);
                    Console.WriteLine($"Deleted game db: {gamePath}");
                }
                else
                {
                    Console.WriteLine($"Game db not found: {gamePath}");
                }

                // Best-effort re-init
                await _gameRepo.InitializeAsync(ct);
                Console.WriteLine("Game db reinitialized.");
                return 0;
            }

            case "wipe-srd":
            {
                var sql = Environment.GetEnvironmentVariable("SRD_SQL_CONNECTION_STRING");
                if (!string.IsNullOrWhiteSpace(sql))
                {
                    Console.WriteLine("SRD is configured for SQL Server. Refusing to wipe via CLI.");
                    return 1;
                }

                var srdPath = Environment.GetEnvironmentVariable("SRD_SQLITE_PATH") ?? Path.Combine(AppContext.BaseDirectory, "data", "srd.sqlite");
                if (File.Exists(srdPath))
                {
                    File.Delete(srdPath);
                    Console.WriteLine($"Deleted SRD sqlite: {srdPath}");
                }
                else
                {
                    Console.WriteLine($"SRD sqlite not found: {srdPath}");
                }

                await _srdRepo.InitializeAsync(ct);
                Console.WriteLine("SRD repo reinitialized.");
                return 0;
            }

            case "seed":
            {
                await SeedTestDataAsync(ct);
                return 0;
            }

            case "init":
            {
                // Recreate game db + seed + ensure SRD source exists and populate SRD cache from Open5e.
                // This is for local testing/dev (no server required).
                var gamePath = Environment.GetEnvironmentVariable("SS_GAME_SQLITE_PATH") ?? Path.Combine(AppContext.BaseDirectory, "data", "game.sqlite");
                if (File.Exists(gamePath)) File.Delete(gamePath);
                await _gameRepo.InitializeAsync(ct);

                // Wipe SRD sqlite only when not SQL Server
                var sql = Environment.GetEnvironmentVariable("SRD_SQL_CONNECTION_STRING");
                if (string.IsNullOrWhiteSpace(sql))
                {
                    var srdPath = Environment.GetEnvironmentVariable("SRD_SQLITE_PATH") ?? Path.Combine(AppContext.BaseDirectory, "data", "srd.sqlite");
                    if (File.Exists(srdPath)) File.Delete(srdPath);
                }

                await _srdRepo.InitializeAsync(ct);

                await EnsureOpen5eAndIngestAsync(ct);
                await SeedTestDataAsync(ct);

                Console.WriteLine("Dev init complete.");
                return 0;
            }

            default:
                Console.WriteLine("dev commands: init | reset | seed | wipe-srd");
                return 1;
        }
    }

    private async Task EnsureOpen5eAndIngestAsync(CancellationToken ct)
    {
        // Ensure default Open5e source + mapping profiles exist (hard-coded base url).
        await SilverSpires.Tactics.Srd.IngestionModule.Ingestion.Open5eBootstrap.EnsureRegisteredAsync(_srdRepo, ct);

        // Local ingestion pass (uses repo + mapping profiles created above).
        // This is intentionally hard-coded to Open5e as the default source for testing.
        var readers = new SilverSpires.Tactics.Srd.Ingestion.Sources.Json.DefaultSourceReaderFactory(new HttpClient());
        var mapper = new SilverSpires.Tactics.Srd.Ingestion.Mapping.GenericMappingEngine();
        var ingestion = new SilverSpires.Tactics.Srd.Ingestion.Ingestion.SrdIngestionService(_srdRepo, readers, mapper);
        var updater = new SilverSpires.Tactics.Srd.IngestionModule.Ingestion.SrdUpdater(ingestion);

        await updater.UpdateAllEnabledSourcesAsync(ct);
        Console.WriteLine("Local Open5e ingestion complete.");
    }

    private async Task SeedTestDataAsync(CancellationToken ct)
    {
        await _gameRepo.InitializeAsync(ct);
        await _srdRepo.InitializeAsync(ct);

        var catalog = await LoadSrdCatalogAsync(ct);

        // Hard-coded test character (basic warrior) using SRD ids where possible.
        var armor = catalog.Armor.FirstOrDefault();
        var weapon = catalog.Weapons.FirstOrDefault();

        var c = new CharacterRecord(
            Guid.Empty,
            "Test Hero",
            "Seeded test character",
            catalog.Classes.FirstOrDefault()?.Id,
            catalog.Races.FirstOrDefault()?.Id,
            3,
            16, 12, 14, 10, 10, 12,
            armor?.Id ?? "srd_chain_mail",
            weapon?.Id ?? "srd_longsword",
            default,
            default);

        var createdChar = await _gameRepo.CreateCharacterAsync(c, ct);

        // Hard-coded test encounter: 2 random monsters
        var monster = catalog.Monsters.FirstOrDefault();
        if (monster is null)
        {
            Console.WriteLine("No monsters found in SRD cache; seed skipped for encounter/campaign.");
            return;
        }

        var enc = await _gameRepo.CreateEncounterAsync(new EncounterRecord(Guid.Empty, "Test Encounter", "Seeded test encounter", default, default), ct);
        await _gameRepo.SetEncounterMonstersAsync(enc.Id,
            new[] { new EncounterMonsterRecord(enc.Id, monster.Id, 2) },
            ct);
        await _gameRepo.SetEncounterCharactersAsync(enc.Id, new[] { createdChar.Id }, ct);

        // Campaign ties them together
        var camp = await _gameRepo.CreateCampaignAsync("Test Campaign", "Seeded test campaign", ct);
        await _gameRepo.SetCampaignCharactersAsync(camp.Id, new[] { createdChar.Id }, ct);
        await _gameRepo.SetCampaignEncountersAsync(camp.Id, new[] { enc.Id }, ct);
        await _gameRepo.SetSettingAsync("campaign.currentId", camp.Id.ToString(), ct);

        Console.WriteLine($"Seeded: Character={createdChar.Id}, Encounter={enc.Id}, Campaign={camp.Id} (set current)");
    }

private static void PrintHelp()
    {
        Console.WriteLine(@"
SilverSpires CLI

Env:
  SRD_API_BASE_URL       -> API base url (default: http://localhost:5188)
  SRD_API_KEY            -> API key for admin/upload endpoints (X-API-Key)
  SS_GAME_SQLITE_PATH    -> game DB path (default: ./data/game.sqlite)
  SRD_SQL_CONNECTION_STRING  -> SRD DB (SQL Server) if set
  SRD_SQLITE_PATH            -> SRD DB (SQLite) if SQL conn not set

Commands:
  srd manifest
  srd sync [--since <ISO8601>]
  server refresh-srd

  character list
  character new
  encounter list
  encounter new
  campaign list
  campaign new
  campaign load
  campaign start [--id <GUID>]

  dev init
  dev reset
  dev seed
  dev wipe-srd

Flow (typical):
  1) server refresh-srd
  2) srd sync
  3) character new
  4) encounter new
  5) campaign new
  6) campaign start
");
    }

    private static void PrintSrdHelp()
        => Console.WriteLine("srd commands: manifest | sync [--since <ISO8601>]");

    private static void PrintServerHelp()
        => Console.WriteLine("server commands: refresh-srd");

    private static void PrintCampaignHelp()
        => Console.WriteLine("campaign commands: list | new | load | start [--id <GUID>]");

    private static void PrintEncounterHelp()
        => Console.WriteLine("encounter commands: list | new");

    private static void PrintCharacterHelp()
        => Console.WriteLine("character commands: list | new");
}
