# SilverSpires.Tactics.Srd.Ingestion (Generic)

This project is a **generic ingestion + mapping + storage** module for SRD-ish content.

## Design goals
- **No hard-coded external sources** (no Open5e-specific client, no baked-in SRD dataset).
- Sources are defined entirely via a **Source Registry** persisted locally (SQLite/SQL Server).
- Each source can have any number of **entity feeds** (monsters, spells, items, etc).
- Mapping is controlled by **Mapping Profiles** stored in the DB, not by code.
- Results are normalized into your canonical domain types in `SilverSpires.Tactics.Srd.*`
  and persisted locally.

## What the module provides
- Source registry entities + storage (SQLite and SQL Server repositories)
- Generic readers:
  - Local JSON file (array or `{ results: [...] }`)
  - HTTP JSON endpoint (same shapes)
- Generic mapping engine:
  - Field approximation via **fallback source paths**
  - Type conversion (string enums, numbers, arrays, nested objects)
  - Simple transforms (lowercase, uppercase, trim, parse CR, parse damage type)
- Ingestion orchestrator to:
  - pull raw JSON from a configured source feed
  - map to canonical SRD models
  - upsert into local storage
- JSON exporter that writes back to `SilverSpires.Tactics.Srd/Data/Json/*.json`

## Next extension points (intentionally left for you)
- More transforms (dice parsing, markdown extraction, computed stats)
- Advanced matching/merge rules (prefer local overrides vs prefer upstream)
- Rich querying (JSON_VALUE indexes in SQL Server, SQLite JSON functions, etc.)
