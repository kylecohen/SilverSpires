using Microsoft.AspNetCore.Http.Features;
using SilverSpires.Tactics.Api.Admin;
using SilverSpires.Tactics.Srd.IngestionModule.Ingestion;
using SilverSpires.Tactics.Api;
using SilverSpires.Tactics.Srd.Persistence.Storage;

using SilverSpires.Tactics.Srd.Characters;
using SilverSpires.Tactics.Srd.Items;
using SilverSpires.Tactics.Srd.Monsters;
using SilverSpires.Tactics.Srd.Rules;
using SilverSpires.Tactics.Srd.Spells;

var builder = WebApplication.CreateBuilder(args);

// Upload sizing (internal bundles)
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 1024L * 1024L * 200L; // 200MB
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ISrdRepository>(_ => ISrdRepository.CreateRepository());



// -----------------------
// Admin jobs + refresh SRD
// -----------------------
builder.Services.AddSingleton<IAdminJobStore>(sp =>
{
    var sqlServer = Environment.GetEnvironmentVariable("SRD_SQL_CONNECTION_STRING");
    if (!string.IsNullOrWhiteSpace(sqlServer))
        return new SqlServerAdminJobStore(sqlServer);

    var sqlite = Environment.GetEnvironmentVariable("SRD_SQLITE_PATH") ?? Path.Combine(AppContext.BaseDirectory, "data", "srd.sqlite");
    return new SqliteAdminJobStore(sqlite);
});

builder.Services.AddSingleton<RefreshSrdJobManager>();
var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    name = "SilverSpires.Tactics.Api",
    ok = true,
    utc = DateTime.UtcNow
}));

app.MapGet("/health", async (ISrdRepository repo, CancellationToken ct) =>
{
    await repo.InitializeAsync(ct);
    return Results.Ok(new { ok = true });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// -----------------------
// Simple API key gate (upload only)
// -----------------------
static bool IsUploadAuthorized(HttpRequest req)
{
    // Set SRD_API_KEY in environment for enforcement.
    var expected = Environment.GetEnvironmentVariable("SRD_API_KEY");

    // If not set, allow (dev convenience), but you should set it before any real deployment.
    if (string.IsNullOrWhiteSpace(expected))
        return true;

    if (!req.Headers.TryGetValue("X-API-Key", out var provided))
        return false;

    return string.Equals(provided.ToString(), expected, StringComparison.Ordinal);
}

static IResult UploadUnauthorized()
    => Results.Unauthorized();

// -----------------------
// Helpers
// -----------------------
static async Task EnsureInit(ISrdRepository repo, CancellationToken ct)
    => await repo.InitializeAsync(ct);

static DateTime? ParseUpdatedSince(string? s)
{
    if (string.IsNullOrWhiteSpace(s)) return null;
    if (DateTime.TryParse(s, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
        return dt.ToUniversalTime();
    return null;
}

static (int page, int pageSize) ParsePaging(int? page, int? pageSize)
{
    var p = page ?? 0;
    var ps = pageSize ?? 200;
    if (p < 0) p = 0;
    if (ps < 1) ps = 1;
    if (ps > 2000) ps = 2000;
    return (p, ps);
}

// -----------------------
// Manifest (sync hint)
// -----------------------
app.MapGet("/api/srd/manifest", async (ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);

    var latest = await repo.GetLatestEntityUpdatedUtcAsync(ct);

    // Counts (cheap enough for now; optimize later if needed)
    var classes = await repo.GetAllClassesAsync(ct);
    var races = await repo.GetAllRacesAsync(ct);
    var backgrounds = await repo.GetAllBackgroundsAsync(ct);
    var feats = await repo.GetAllFeatsAsync(ct);
    var skills = await repo.GetAllSkillsAsync(ct);
    var languages = await repo.GetAllLanguagesAsync(ct);
    var spells = await repo.GetAllSpellsAsync(ct);
    var monsters = await repo.GetAllMonstersAsync(ct);
    var magicItems = await repo.GetAllMagicItemsAsync(ct);
    var equipment = await repo.GetAllEquipmentAsync(ct);
    var weapons = await repo.GetAllWeaponsAsync(ct);
    var armor = await repo.GetAllArmorAsync(ct);
    var effects = await repo.GetAllEffectsAsync(ct);

    return Results.Ok(new
    {
        generatedUtc = DateTime.UtcNow,
        latestEntityUpdatedUtc = latest,
        counts = new
        {
            classes = classes.Count,
            races = races.Count,
            backgrounds = backgrounds.Count,
            feats = feats.Count,
            skills = skills.Count,
            languages = languages.Count,
            spells = spells.Count,
            monsters = monsters.Count,
            magicItems = magicItems.Count,
            equipment = equipment.Count,
            weapons = weapons.Count,
            armor = armor.Count,
            effects = effects.Count
        }
    });
});

// -----------------------
// SRD retrieval endpoints (typed)
// NOTE: These are simple and intentionally "dumb": load all, filter in memory.
// Add proper DB-level lookups later if performance needs it.
// -----------------------
app.MapGet("/api/srd/classes", async (ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    return Results.Ok(await repo.GetAllClassesAsync(ct));
});

app.MapGet("/api/srd/classes/{id}", async (string id, ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    var all = await repo.GetAllClassesAsync(ct);
    var x = all.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
    return x is null ? Results.NotFound() : Results.Ok(x);
});

app.MapGet("/api/srd/races", async (ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    return Results.Ok(await repo.GetAllRacesAsync(ct));
});

app.MapGet("/api/srd/races/{id}", async (string id, ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    var all = await repo.GetAllRacesAsync(ct);
    var x = all.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
    return x is null ? Results.NotFound() : Results.Ok(x);
});

app.MapGet("/api/srd/backgrounds", async (ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    return Results.Ok(await repo.GetAllBackgroundsAsync(ct));
});

app.MapGet("/api/srd/backgrounds/{id}", async (string id, ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    var all = await repo.GetAllBackgroundsAsync(ct);
    var x = all.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
    return x is null ? Results.NotFound() : Results.Ok(x);
});

app.MapGet("/api/srd/feats", async (ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    return Results.Ok(await repo.GetAllFeatsAsync(ct));
});

app.MapGet("/api/srd/feats/{id}", async (string id, ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    var all = await repo.GetAllFeatsAsync(ct);
    var x = all.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
    return x is null ? Results.NotFound() : Results.Ok(x);
});

app.MapGet("/api/srd/skills", async (ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    return Results.Ok(await repo.GetAllSkillsAsync(ct));
});

app.MapGet("/api/srd/skills/{id}", async (string id, ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    var all = await repo.GetAllSkillsAsync(ct);
    var x = all.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
    return x is null ? Results.NotFound() : Results.Ok(x);
});

app.MapGet("/api/srd/languages", async (ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    return Results.Ok(await repo.GetAllLanguagesAsync(ct));
});

app.MapGet("/api/srd/languages/{id}", async (string id, ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    var all = await repo.GetAllLanguagesAsync(ct);
    var x = all.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
    return x is null ? Results.NotFound() : Results.Ok(x);
});

app.MapGet("/api/srd/spells", async (ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    return Results.Ok(await repo.GetAllSpellsAsync(ct));
});

app.MapGet("/api/srd/spells/{id}", async (string id, ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    var all = await repo.GetAllSpellsAsync(ct);
    var x = all.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
    return x is null ? Results.NotFound() : Results.Ok(x);
});

app.MapGet("/api/srd/monsters", async (ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    return Results.Ok(await repo.GetAllMonstersAsync(ct));
});

app.MapGet("/api/srd/monsters/{id}", async (string id, ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    var all = await repo.GetAllMonstersAsync(ct);
    var x = all.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
    return x is null ? Results.NotFound() : Results.Ok(x);
});

app.MapGet("/api/srd/magic-items", async (ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    return Results.Ok(await repo.GetAllMagicItemsAsync(ct));
});

app.MapGet("/api/srd/magic-items/{id}", async (string id, ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    var all = await repo.GetAllMagicItemsAsync(ct);
    var x = all.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
    return x is null ? Results.NotFound() : Results.Ok(x);
});

app.MapGet("/api/srd/equipment", async (ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    return Results.Ok(await repo.GetAllEquipmentAsync(ct));
});

app.MapGet("/api/srd/equipment/{id}", async (string id, ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    var all = await repo.GetAllEquipmentAsync(ct);
    var x = all.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
    return x is null ? Results.NotFound() : Results.Ok(x);
});

app.MapGet("/api/srd/weapons", async (ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    return Results.Ok(await repo.GetAllWeaponsAsync(ct));
});

app.MapGet("/api/srd/weapons/{id}", async (string id, ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    var all = await repo.GetAllWeaponsAsync(ct);
    var x = all.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
    return x is null ? Results.NotFound() : Results.Ok(x);
});

app.MapGet("/api/srd/armor", async (ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    return Results.Ok(await repo.GetAllArmorAsync(ct));
});

app.MapGet("/api/srd/armor/{id}", async (string id, ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    var all = await repo.GetAllArmorAsync(ct);
    var x = all.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
    return x is null ? Results.NotFound() : Results.Ok(x);
});

app.MapGet("/api/srd/effects", async (ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    return Results.Ok(await repo.GetAllEffectsAsync(ct));
});

app.MapGet("/api/srd/effects/{id}", async (string id, ISrdRepository repo, CancellationToken ct) =>
{
    await EnsureInit(repo, ct);
    var all = await repo.GetAllEffectsAsync(ct);
    var x = all.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
    return x is null ? Results.NotFound() : Results.Ok(x);
});

// -----------------------
// SRD sync endpoints (raw, paged, updatedSince)
// -----------------------
app.MapGet("/api/srd/sync/{entityType}", async (
    string entityType,
    string? updatedSinceUtc,
    int? page,
    int? pageSize,
    ISrdRepository repo,
    CancellationToken ct) =>
{
    await EnsureInit(repo, ct);

    var since = ParseUpdatedSince(updatedSinceUtc);
    var (p, ps) = ParsePaging(page, pageSize);

    var batch = await repo.GetEntityBatchAsync(entityType, since, p, ps, ct);
    return Results.Ok(batch);
});

// -----------------------
// Internal upload endpoints (typed lists)
// NOTE: Add real auth (JWT) later; for now, API key header gate.
// Header: X-API-Key: <SRD_API_KEY>
// -----------------------
app.MapPost("/api/srd/upload/classes", async (HttpRequest request, ISrdRepository repo, CancellationToken ct) =>
{
    if (!IsUploadAuthorized(request)) return UploadUnauthorized();
    await EnsureInit(repo, ct);

    var items = await request.ReadFromJsonAsync<List<SrdClass>>(cancellationToken: ct);
    if (items is null) return Results.BadRequest("Expected JSON array of classes.");

    foreach (var e in items) await repo.UpsertClassAsync(e, ct);
    return Results.Ok(new { upserted = items.Count });
});

app.MapPost("/api/srd/upload/races", async (HttpRequest request, ISrdRepository repo, CancellationToken ct) =>
{
    if (!IsUploadAuthorized(request)) return UploadUnauthorized();
    await EnsureInit(repo, ct);

    var items = await request.ReadFromJsonAsync<List<SrdRace>>(cancellationToken: ct);
    if (items is null) return Results.BadRequest("Expected JSON array of races.");

    foreach (var e in items) await repo.UpsertRaceAsync(e, ct);
    return Results.Ok(new { upserted = items.Count });
});

app.MapPost("/api/srd/upload/backgrounds", async (HttpRequest request, ISrdRepository repo, CancellationToken ct) =>
{
    if (!IsUploadAuthorized(request)) return UploadUnauthorized();
    await EnsureInit(repo, ct);

    var items = await request.ReadFromJsonAsync<List<SrdBackground>>(cancellationToken: ct);
    if (items is null) return Results.BadRequest("Expected JSON array of backgrounds.");

    foreach (var e in items) await repo.UpsertBackgroundAsync(e, ct);
    return Results.Ok(new { upserted = items.Count });
});

app.MapPost("/api/srd/upload/feats", async (HttpRequest request, ISrdRepository repo, CancellationToken ct) =>
{
    if (!IsUploadAuthorized(request)) return UploadUnauthorized();
    await EnsureInit(repo, ct);

    var items = await request.ReadFromJsonAsync<List<SrdFeat>>(cancellationToken: ct);
    if (items is null) return Results.BadRequest("Expected JSON array of feats.");

    foreach (var e in items) await repo.UpsertFeatAsync(e, ct);
    return Results.Ok(new { upserted = items.Count });
});

app.MapPost("/api/srd/upload/skills", async (HttpRequest request, ISrdRepository repo, CancellationToken ct) =>
{
    if (!IsUploadAuthorized(request)) return UploadUnauthorized();
    await EnsureInit(repo, ct);

    var items = await request.ReadFromJsonAsync<List<SrdSkill>>(cancellationToken: ct);
    if (items is null) return Results.BadRequest("Expected JSON array of skills.");

    foreach (var e in items) await repo.UpsertSkillAsync(e, ct);
    return Results.Ok(new { upserted = items.Count });
});

app.MapPost("/api/srd/upload/languages", async (HttpRequest request, ISrdRepository repo, CancellationToken ct) =>
{
    if (!IsUploadAuthorized(request)) return UploadUnauthorized();
    await EnsureInit(repo, ct);

    var items = await request.ReadFromJsonAsync<List<SrdLanguage>>(cancellationToken: ct);
    if (items is null) return Results.BadRequest("Expected JSON array of languages.");

    foreach (var e in items) await repo.UpsertLanguageAsync(e, ct);
    return Results.Ok(new { upserted = items.Count });
});

app.MapPost("/api/srd/upload/spells", async (HttpRequest request, ISrdRepository repo, CancellationToken ct) =>
{
    if (!IsUploadAuthorized(request)) return UploadUnauthorized();
    await EnsureInit(repo, ct);

    var items = await request.ReadFromJsonAsync<List<SrdSpell>>(cancellationToken: ct);
    if (items is null) return Results.BadRequest("Expected JSON array of spells.");

    foreach (var e in items) await repo.UpsertSpellAsync(e, ct);
    return Results.Ok(new { upserted = items.Count });
});

app.MapPost("/api/srd/upload/monsters", async (HttpRequest request, ISrdRepository repo, CancellationToken ct) =>
{
    if (!IsUploadAuthorized(request)) return UploadUnauthorized();
    await EnsureInit(repo, ct);

    var items = await request.ReadFromJsonAsync<List<SrdMonster>>(cancellationToken: ct);
    if (items is null) return Results.BadRequest("Expected JSON array of monsters.");

    foreach (var e in items) await repo.UpsertMonsterAsync(e, ct);
    return Results.Ok(new { upserted = items.Count });
});

app.MapPost("/api/srd/upload/magic-items", async (HttpRequest request, ISrdRepository repo, CancellationToken ct) =>
{
    if (!IsUploadAuthorized(request)) return UploadUnauthorized();
    await EnsureInit(repo, ct);

    var items = await request.ReadFromJsonAsync<List<SrdMagicItem>>(cancellationToken: ct);
    if (items is null) return Results.BadRequest("Expected JSON array of magic items.");

    foreach (var e in items) await repo.UpsertMagicItemAsync(e, ct);
    return Results.Ok(new { upserted = items.Count });
});

app.MapPost("/api/srd/upload/equipment", async (HttpRequest request, ISrdRepository repo, CancellationToken ct) =>
{
    if (!IsUploadAuthorized(request)) return UploadUnauthorized();
    await EnsureInit(repo, ct);

    var items = await request.ReadFromJsonAsync<List<SrdEquipment>>(cancellationToken: ct);
    if (items is null) return Results.BadRequest("Expected JSON array of equipment.");

    foreach (var e in items) await repo.UpsertEquipmentAsync(e, ct);
    return Results.Ok(new { upserted = items.Count });
});

app.MapPost("/api/srd/upload/weapons", async (HttpRequest request, ISrdRepository repo, CancellationToken ct) =>
{
    if (!IsUploadAuthorized(request)) return UploadUnauthorized();
    await EnsureInit(repo, ct);

    var items = await request.ReadFromJsonAsync<List<SrdWeapon>>(cancellationToken: ct);
    if (items is null) return Results.BadRequest("Expected JSON array of weapons.");

    foreach (var e in items) await repo.UpsertWeaponAsync(e, ct);
    return Results.Ok(new { upserted = items.Count });
});

app.MapPost("/api/srd/upload/armor", async (HttpRequest request, ISrdRepository repo, CancellationToken ct) =>
{
    if (!IsUploadAuthorized(request)) return UploadUnauthorized();
    await EnsureInit(repo, ct);

    var items = await request.ReadFromJsonAsync<List<SrdArmor>>(cancellationToken: ct);
    if (items is null) return Results.BadRequest("Expected JSON array of armor.");

    foreach (var e in items) await repo.UpsertArmorAsync(e, ct);
    return Results.Ok(new { upserted = items.Count });
});

app.MapPost("/api/srd/upload/effects", async (HttpRequest request, ISrdRepository repo, CancellationToken ct) =>
{
    if (!IsUploadAuthorized(request)) return UploadUnauthorized();
    await EnsureInit(repo, ct);

    var items = await request.ReadFromJsonAsync<List<GameEffect>>(cancellationToken: ct);
    if (items is null) return Results.BadRequest("Expected JSON array of effects.");

    foreach (var e in items) await repo.UpsertEffectAsync(e, ct);
    return Results.Ok(new { upserted = items.Count });
});



// -----------------------
// Admin endpoints
// -----------------------
app.MapPost("/api/admin/refresh-srd", async (HttpContext ctx, RefreshSrdJobManager jobs, CancellationToken ct) =>
{
    if (!IsUploadAuthorized(ctx.Request)) return UploadUnauthorized();

    var job = await jobs.CreateQueuedAsync(ct);

    // fire-and-forget on threadpool; state is persisted via job store
    _ = Task.Run(() => jobs.RunAsync(job.Id, ct), CancellationToken.None);

    return Results.Accepted($"/api/admin/jobs/{job.Id}", job);
});

app.MapGet("/api/admin/jobs/{id:guid}", async (HttpContext ctx, Guid id, RefreshSrdJobManager jobs, CancellationToken ct) =>
{
    if (!IsUploadAuthorized(ctx.Request)) return UploadUnauthorized();

    var job = await jobs.GetAsync(id, ct);
    return job is null ? Results.NotFound() : Results.Ok(job);
});

app.Run();
