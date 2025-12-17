using SilverSpires.Tactics.Srd.Ingestion.Ingestion;
using SilverSpires.Tactics.Srd.Ingestion.Mapping;
using SilverSpires.Tactics.Srd.Ingestion.Sources.Json;
using SilverSpires.Tactics.Srd.IngestionModule.Ingestion;
using SilverSpires.Tactics.Srd.Persistence.Storage;
using SilverSpires.Tactics.Srd.Persistence.Storage.SqlServer;
using SilverSpires.Tactics.Srd.Persistence.Storage.Sqlite;

if (args.Length == 0)
{
    PrintHelp();
    return;
}

var repo = ISrdRepository.CreateRepository();
await repo.InitializeAsync();

var http = new HttpClient();
var readers = new DefaultSourceReaderFactory(http);
var mapper = new GenericMappingEngine();
var ingestion = new SrdIngestionService(repo, readers, mapper);

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
