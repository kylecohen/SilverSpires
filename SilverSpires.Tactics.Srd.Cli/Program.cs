using SilverSpires.Tactics.Game;
using SilverSpires.Tactics.Srd.Cli;
using SilverSpires.Tactics.Srd.IngestionModule.Ingestion;
using SilverSpires.Tactics.Srd.Persistence.Storage;

var ct = CancellationToken.None;

// Shared SRD repository (local cache)
var srdRepo = ISrdRepository.CreateRepository();
await srdRepo.InitializeAsync(ct);

// Game DB repository (campaigns, encounters, characters)
var gamePath = Environment.GetEnvironmentVariable("SS_GAME_SQLITE_PATH") ?? Path.Combine(AppContext.BaseDirectory, "data", "game.sqlite");
var gameRepo = new SqliteGameRepository(gamePath);
await gameRepo.InitializeAsync(ct);

// API client
var baseUrl = Environment.GetEnvironmentVariable("SRD_API_BASE_URL") ?? "http://localhost:5188";
var api = new HttpClient { BaseAddress = new Uri(baseUrl) };

// New CLI app (production layout)
var app = new CliApp(srdRepo, gameRepo, api);
var code = await app.RunAsync(args, ct);

// Legacy fallback: if app returns 2, run legacy ingestion commands (local ingestion)
if (code == 2)
{
    // Existing behavior: local ingestion into the configured repo.
    // Kept for dev/offline use, but primary flow is server refresh-srd + srd sync.
    await RunLegacyIngestionAsync(args, srdRepo, ct);
    code = 0;
}

var repo = ISrdRepository.CreateRepository();
await repo.InitializeAsync();

static async Task RunLegacyIngestionAsync(string[] args, ISrdRepository repo, CancellationToken ct)
{
    if (args.Length == 0) return;

    var cmd = args[0].ToLowerInvariant();

    if (cmd == "bootstrap" && args.Length >= 2 && args[1].Equals("open5e", StringComparison.OrdinalIgnoreCase))
    {
        await Open5eBootstrap.EnsureRegisteredAsync(repo);
        Console.WriteLine("Bootstrapped Open5e.");
        return;
    }

    if (cmd == "sources" && args.Length >= 2 && args[1].Equals("list", StringComparison.OrdinalIgnoreCase))
    {
        var sources = await repo.GetSourcesAsync();
        foreach (var s in sources)
            Console.WriteLine($"{s.Id}\t{s.Name}\t{s.Kind}\tEnabled={s.IsEnabled}");
        return;
    }

    if (cmd == "update")
    {
        // Wire ingestion in-process
        var readers = new SilverSpires.Tactics.Srd.Ingestion.Sources.Json.DefaultSourceReaderFactory(new HttpClient());
        var mapper = new SilverSpires.Tactics.Srd.Ingestion.Mapping.GenericMappingEngine();
        var ingestion = new SilverSpires.Tactics.Srd.Ingestion.Ingestion.SrdIngestionService(repo, readers, mapper);
        var updater = new SrdUpdater(ingestion);

        if (args.Length >= 2 && args[1].Equals("--ensure-open5e", StringComparison.OrdinalIgnoreCase))
            await Open5eBootstrap.EnsureRegisteredAsync(repo);

        var report = await ingestion.IngestAllEnabledSourcesAsync("cli");
        Console.WriteLine(report.ToString());

        if (report.Errors.Count > 0)
        {
            Console.WriteLine("Errors:");
            foreach (var e in report.Errors) Console.WriteLine(e);
            Environment.ExitCode = 2;
        }
        return;
    }

    PrintHelp();

    static void PrintHelp()
    {
        Console.WriteLine(@"
            SilverSpires SRD CLI

            Env:
              SRD_SQL_CONNECTION_STRING  -> uses SQL Server/Azure SQL if set
              SRD_SQLITE_PATH            -> sqlite file path if SQL conn not set

            Commands:
              bootstrap open5e                 Register Open5e source+feeds+profiles in DB
              sources list                     List registered sources
              update [--ensure-open5e]         Ingest all enabled sources/feeds into canonical SRD entities
        ");
    }
}
