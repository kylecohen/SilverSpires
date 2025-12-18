# Unity + API Hybrid Plan

You asked for a combined approach:
- Server-side API hosts canonical + approved custom SRD data (SQL Server later; SQLite now).
- Client-side Unity app ships with a local SQLite cache for offline play and fast lookup.
- Client can periodically sync from the API.

## What this commit adds
- `SilverSpires.Tactics.Api` (ASP.NET Core minimal API)
  - Swagger enabled in Development
  - `/api/srd/*` retrieval endpoints for Monsters + Spells (expand as needed)
  - `/api/srd/upload/*` internal upload endpoints for Monsters + Spells (add auth ASAP)

## Running the API
From repo root:
```bash
dotnet run --project SilverSpires.Tactics.Api
```

Optional env vars:
- `SRD_SQL_CONNECTION_STRING` => uses SQL Server
- `SRD_SQLITE_PATH` => overrides sqlite path (default: ./srd_cache.sqlite next to app)

## Unity client sync strategy (recommended)
- Bundle an initial `srd_cache.sqlite` in Unity `StreamingAssets/`
- On first launch, copy to `Application.persistentDataPath`
- Use a Unity-friendly SQLite plugin to read/write (IL2CPP-safe)
- Sync:
  1. call `/api/srd/manifest` to check counts/version (later: add hashes/updatedUtc)
  2. if needs update, download entities in pages and upsert locally

Next step: add paging + `updatedSinceUtc` to API, and add an API key/JWT to upload endpoints.

## Upload auth
Set an API key before any deployment:
- Environment: `SRD_API_KEY`
- Header: `X-API-Key: <value>`

## Raw sync endpoint
`GET /api/srd/sync/{entityType}?updatedSinceUtc=<ISO8601>&page=<n>&pageSize=<n>`
Returns a list of `SrdEntityEnvelope` (EntityType, Id, Json, UpdatedUtc).

## Client sync helper
See `SilverSpires.Tactics/Sync/SrdSyncClient.cs`.
It pulls from the raw sync endpoint and upserts into a local `ISrdRepository`.
Persist the returned timestamp as your next `lastSyncUtc`.
