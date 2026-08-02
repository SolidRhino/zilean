# Repository Guidelines

> Practical guide for AI assistants working in the Zilean codebase.

## Project Overview

Zilean is a **Torznab indexer** for [DebridMediaManager](https://github.com/debridmediamanager/debrid-media-manager) (DMM) sourced content shared by users. It exposes a single Torznab API consumable by Prowlarr, Sonarr, Radarr, Shelfarr, and other *arr apps, and can also scrape from a running Zurg instance or other Zilean instances.

- **Fork**: `SolidRhino/zilean`, tracking upstream [`Thoroslives/zilean`](https://github.com/Thoroslives/zilean). Status: **no longer actively maintained**. Published image `ghcr.io/solidrhino/zilean:latest`. Branch `main` is protected by a ruleset (`protect-main`): direct pushes blocked, PRs require `Conventional Commits` + `build-and-test` checks to pass, no bypass.
- **Stack**: .NET 9 (ASP.NET Core + EF Core 9), PostgreSQL 16+ with `pg_trgm` + `unaccent` (Elasticsearch was removed in v2.0). Python 3.12 embedded via pythonnet for RTN parsing.
- **Categories**: Movies `2000`, TV `5000`, Books `7000`, Audiobooks `3030`, XXX `6000`. Books/audiobooks detected by post-RTN heuristics (extension + title keywords).

## Architecture & Data Flow

Two runnable hosts sharing `Zilean.Database` + `Zilean.Shared`:

```
┌─────────────────────────┐        ┌──────────────────────────┐
│  Zilean.ApiService      │        │  Zilean.Scraper (CLI)    │
│  (WebApplication, 8181) │        │  Spectre.Console cmds    │
│  - Torznab API          │        │  - dmm-sync              │
│  - /dmm search          │        │  - generic-sync          │
│  - Health checks        │        │  - resync-imdb           │
│  - Blazor dashboard     │        │                          │
└──────────┬──────────────┘        └───────────┬──────────────┘
           │  Coravel schedules                │  CLI shells
           │  DmmSyncJob/GenericSyncJob        │  executed by ApiService
           └──────────────┬────────────────────┘
                          ▼
            ┌──────────────────────────────┐
            │  Zilean.Database (EF Core)   │
            │  ZileanDbContext + EF Core   │
            │  PostgreSQL 16 (pg_trgm)     │
            └──────────────────────────────┘
```

**Request flow (ApiService):** `Program.cs` → `AddConfiguration` + `AddZileanDataServices` (registers `ZileanDbContext` via `AddDbContextFactory` with `UseNpgsql` — both `IDbContextFactory<ZileanDbContext>` singleton and scoped `ZileanDbContext`, plus `ITorrentInfoService`, `IImdbFileService`, `IBlacklistService`, `ITorrentsQueryService`, `IImdbMatchingService` singleton) + `AddApiKeyAuthentication` + `AddSchedulingSupport` (Coravel) + `AddDashboardSupport` (Blazor + Syncfusion). `WebApplicationExtensions.MapZileanEndpoints` chains `Map{Dmm,Imdb,Torznab,Torrents,Blacklist,HealthCheck}Endpoints`. Each is a static class under `Features/` exposing `Map<Name>Endpoints(this WebApplication, ZileanConfiguration)` that conditionally maps routes based on `configuration.<Area>.EnableEndpoint`. Endpoints resolve services via method-injection params (`[AsParameters] TorznabRequest`, `ITorrentInfoService`, `IBlacklistService`, `ITorrentsQueryService`). Search/torznab endpoints call `torrentInfoService.SearchForTorrentInfoFiltered` → `search_torrents_meta(...)` PG function via Dapper (`BaseDapperService.ExecuteCommandAsync`). IMDb endpoint → `search_imdb_meta(...)`. `/torrents/all` streams via `ITorrentsQueryService.StreamAllAsync` (async iterator). Blacklist endpoints delegate to `IBlacklistService.AddAsync`/`RemoveAsync` and map `BlacklistResult` → HTTP status. `/torrents/checkcached` delegates to `ITorrentsQueryService.CheckCachedAsync`. Auth: `X-API-KEY` header validated by `ApiKeyAuthenticationHandler`; `ApiKeyAuthentication.Policy` required on Torrents/Blacklist/on-demand-scrape.

**Ingestion flow (Scraper):** `Program.cs` builds an `IHost` with Serilog, calls `AddScrapers` (registers `DmmFileDownloader`, `DmmScraping`, `GenericIngestionScraping`, `KubernetesServiceDiscovery`, `ImdbFileDownloader/Processor/MetadataLoader`, `PythonRuntimeService` + `TorrentParser`, plus `AddZileanDataServices` + `AddHostedService<EnsureMigrated>`), then `AddCommandLine<DefaultCommand>` registers Spectre commands `dmm-sync` / `generic-sync` / `resync-imdb`. `EnsureMigrated` runs `MigrateAsync` (via `IDbContextFactory.CreateDbContextAsync`) + loads IMDb metadata. `DmmScraping` downloads the DMM hashlist zip, `DmmFileEntryProcessor` (extends `GenericProcessor<ExtractedDmmEntry>`) reads HTML files, regex-extracts LZ-string-encoded JSON (`Decompressor.FromEncodedUriComponent`), parses via `Utf8JsonReader`, dedupes by InfoHash, feeds a bounded `Channel<Task<TInput>>` (producer/consumer, batch = `Parsing.BatchSize`). Consumer batches call `TorrentParser.ParseAndPopulateAsync` (pythonnet embedding RTN `parse()` — `PythonRuntimeService` owns the engine lifecycle), filter blacklisted hashes, then `torrentInfoService.StoreTorrentInfo` → EFCore.BulkExtensions `BulkInsertOrUpdateAsync` keyed on InfoHash, with optional IMDb matching via singleton `IImdbMatchingService` (Lucene or FuzzySharp). Both syncs finish with `VACUUM (VERBOSE, ANALYZE)`.

**ApiService also ingests indirectly:** Coravel `Schedule<DmmSyncJob>()` / `<GenericSyncJob>()` on cron (`Dmm.ScrapeSchedule`, `Ingestion.ScrapeSchedule`) with `PreventOverlapping("SyncJobs")`; jobs shell-execute the `scraper` binary via `IShellExecutionService` (CliWrap). `StartupService` (`IHostedLifecycleService`) validates config, waits for DB with retry, applies migrations, triggers first-run `DmmSyncJob` if no `ParsedPages` exist. `ConfigurationUpdaterService` persists `settings.json` and regenerates the API key when `ZILEAN__NEW__API__KEY=1`.

**Data model** (`ZileanDbContext`): `Torrents` (`TorrentInfo`), `ImdbFiles` (`ImdbFile`), `ParsedPages`, `ImportMetadata` (JSON doc storing `DmmLastImport`/`ImdbLastImport`), `BlacklistedItems`. `TorrentInfoConfiguration` maps ~50 columns with snake_case `Relational:JsonPropertyName`, GiST trigram index on `CleanedParsedTitle` (`gist_trgm_ops`, KNN distance support), GIN indexes on Seasons/Episodes/Languages, btree on Year/ImdbId/IsAdult/Trash/IngestedAt. SQL functions live as `internal const string` in `src/Zilean.Database/Functions/*` and are applied via migrations (`migrationBuilder.Sql(SearchTorrentsMetaV7.Create)`). Current search fn `search_torrents_meta` (V7) with two-stage GiST KNN ordering (inner `ORDER BY "CleanedParsedTitle" <-> query FETCH FIRST N ROWS WITH TIES` + outer re-sort by distance + `IngestedAt DESC LIMIT N`; branches on `query IS NULL` for recency-only sort) + adaptive similarity threshold for filtered/book/audiobook queries; `search_imdb_meta` (V3) for IMDb.

## Key Directories

| Path | Purpose |
|------|---------|
| `src/Zilean.ApiService/` | ASP.NET Core web host: Torznab API, `/dmm` search, health checks, Blazor dashboard. `Features/` holds endpoint groups (`Dmm`, `Imdb`, `Torznab`, `Torrents`, `Blacklist`, `HealthChecks`). |
| `src/Zilean.Scraper/` | Spectre.Console CLI host. `Features/` holds `Dmm`/`GenericIngestion`/`Imdb` scraping and `Commands/` (dmm-sync, generic-sync, resync-imdb). |
| `src/Zilean.Shared/` | Shared DTOs, configuration, Torznab models, decompression, Python parsing services (`PythonRuntimeService`, `TorrentParser`, `CategoryClassifier`). `Features/{Configuration,Dmm,Imdb,Scraping,Python,Torrents}`. |
| `src/Zilean.Database/` | EF Core `ZileanDbContext` (via `AddDbContextFactory`), entities (`Dtos/`), `ModelConfiguration/`, `Migrations/`, `Functions/` (raw SQL), `Indexes/`, `Bootstrapping/`, `Services/` (EF Core reads/writes via `IDbContextFactory` + `SqlQueryRaw`/`FromSqlRaw`, plus `IBlacklistService`/`ITorrentsQueryService`). |
| `src/Zilean.Benchmarks/` | BenchmarkDotNet project (`PythonParsing.cs`). References `Zilean.Scraper`. |
| `tests/Zilean.Tests/` | xUnit suite. `Fixtures/`, `Collections/`, `Tests/`. |
| `eng/` | Scripts, k6 load tests, dev compose, HTTP files. |
| `docs/Writerside/` | JetBrains Writerside documentation project. |
| `.run/` | Rider run configs (`Compose.run.xml`, stale `Zilean.ImdbLoader.run.xml` from the ES era). |
| `.github/workflows/` | `cicd.yaml` (build/test/release/docker), `conventional-commits.yaml` (PR commit-message gate), `docs.yaml` (`workflow_dispatch`-only Writerside docs deploy). |

## Development Commands

```bash
# Restore / build
dotnet restore
dotnet build -c Release

# Run API (listens on 8181 by default; check Properties/launchSettings.json for local overrides)
dotnet run --project src/Zilean.ApiService

# Run scraper CLI
dotnet run --project src/Zilean.Scraper -- dmm-sync
dotnet run --project src/Zilean.Scraper -- generic-sync
dotnet run --project src/Zilean.Scraper -- resync-imdb -s -t -a

# Tests
dotnet test                                          # full suite (needs Docker + Python for trait tests)
dotnet test --filter "Category!=RequiresPython&Category!=Benchmark"   # CI filter — no Python/Docker-heavy bench
dotnet test --filter "Category=RequiresPython"      # only Python-runtime tests
dotnet test --filter "FullyQualifiedName~ApiIntegrationTests"
dotnet test --collect:"XPlat Code Coverage"         # coverage via coverlet

# Benchmarks (BenchmarkDotNet requires Release + local Python 3.12)
dotnet run --project src/Zilean.Benchmarks -c Release
dotnet run --project src/Zilean.Benchmarks -c Release -- --filter *PythonParsing*

# EF Core migration (helper wraps dotnet ef + renames to custom prefix convention)
cd eng && ./create-new-migration.sh <MigrationName> <CustomPrefix>
#   e.g. ./create-new-migration.sh SearchV8BookThreshold 20260501000000
#   Internally: dotnet ef migrations add <Name> from src/Zilean.Database, then renames
#   Migrations/<timestamp>_<Name>.cs → Migrations/<CustomPrefix>_<Name>.cs and rewrites
#   the [Migration("...")] attribute in the Designer file. Migrations auto-apply on startup.

# Vendor Python deps (loguru, rich, rank-torrent-name) into project-local python/ dirs
./eng/install-python-reqs-dmmscraper.sh     # Zilean.Scraper + Zilean.ApiService
./eng/install-python-reqs-benchmarks.sh     # Zilean.Benchmarks

# Dev database (postgres:17.1; set POSTGRES_PASSWORD env first)
docker compose -f eng/compose-dev.yaml up -d

# Docker image
docker build -t zilean .                                              # local single-arch
docker buildx build --platform linux/amd64,linux/arm64 -t zilean .    # multi-arch (matches CI)

# k6 load tests (API must be running on localhost:8181)
k6 run eng/k6/performance_test.js    # 10→20 VUs / 9m, p(95)<500ms, err<1%
k6 run eng/k6/high_load_test.js      # 50→100→200 VUs / 18m, p(95)<2000ms, err<5%
k6 run eng/k6/stress_test.js         # 100 VUs / 1m, p(95)<2000ms, err<5%
#   All POST {queryText:"iron man 3"} to http://localhost:8181/dmm/search
```

**Lint/format:** no explicit format command in CI. Style enforced at build time via `.editorconfig` + `EnforceCodeStylesInBuild=true` and analyzer rules surfaced as warnings/errors by `dotnet build`. `dotnet build -c Release` is the effective lint gate.

## Code Conventions & Common Patterns

**Project layout:** each project has a `Features/` folder subdivided by capability (`Dmm`, `Imdb`, `Scraping`, `Torznab`, `Torrents`, `Blacklist`, `HealthChecks`, `Configuration`). Every project ships a `GlobalUsings.cs` so feature files rarely need explicit `using` statements for shared imports. Endpoint groups are static classes with `Map<Name>Endpoints(this WebApplication, ZileanConfiguration)` extension methods that conditionally map routes based on `configuration.<Area>.EnableEndpoint`.

**Formatting (from `.editorconfig`):** spaces only; C# indent 4, XML/front-end indent 2; trim trailing whitespace; insert final newline; **max line 130**. **Allman braces** (open brace on new line). Single-line statements/blocks preserved. No space before commas/semicolons/dots/open brackets. **File-scoped namespaces** (`IDE0161 = error`); `using` directives outside namespace.

**Naming:** PascalCase types/methods/properties; PascalCase constants (suggestion); instance fields `_camelCase` (suggestion); `IDE1006` naming violations = error.

**`var`:** preferred when type apparent (suggestion). `IDE0007` disabled, `IDE0008` suggestion.

**Nullability:** `Nullable=enable` project-wide. `CS8618` (non-nullable ctor), `CS8625` (null to non-nullable), `CS8605+` = error; `CS8600–CS8604` suppressed to `none` (pragmatic). Unused locals `CS0168/CS0219` = error. `csharp_prefer_braces = warning`; `csharp_prefer_simple_using_statement = warning`; readonly fields preferred.

**Async:** all I/O and DB calls are `async Task`/`Task<T>` and awaited. Streaming endpoints use `AsAsyncEnumerable()`. Hosted services implement `IHostedLifecycleService`. Tests return `Task` and are awaited.

**Dependency injection:** services registered in `Program.cs` via `AddZileanDataServices` / `AddScrapers` / `AddDashboardSupport` / `AddSchedulingSupport`. EF Core `DbContext` registered via `AddDbContextFactory<ZileanDbContext>` (registers both `IDbContextFactory<ZileanDbContext>` singleton and scoped `ZileanDbContext`). Services that need short-lived contexts outside request scope (singletons like `IImdbMatchingService`, scraper services) inject `IDbContextFactory<ZileanDbContext>` and call `CreateDbContextAsync()`. `IImdbMatchingService` is a singleton (Lucene/FuzzySharp) and injects `IDbContextFactory` directly. Endpoints resolve services via method-injection parameters rather than `HttpContext.RequestServices`. No service uses `IServiceProvider` for DbContext resolution.

**Database access:** EF Core only (via `IDbContextFactory.CreateDbContextAsync()`) for entity CRUD/streaming, `EFCore.BulkExtensions` `BulkInsertOrUpdateAsync` for ingestion, and `Database.SqlQueryRaw<T>` for PostgreSQL function calls (`search_torrents_meta`, `search_imdb_meta`); `FromSqlRaw` for entity-shaped queries (`SearchForTorrentInfoByOnlyTitle`). A flat `TorrentInfoQueryDto` maps `search_torrents_meta` results without the `Imdb` navigation property that `SqlQueryRaw` rejects. Singleton IMDb matchers (`ImdbLuceneMatchingService`, `ImdbFuzzyStringMatchingService`) use `IDbContextFactory` + `FromSqlRaw` for bulk loads (preserving server-side normalization SQL). Raw SQL functions are `internal const string` in `src/Zilean.Database/Functions/*` applied via `migrationBuilder.Sql(...)`. Migrations **auto-apply on startup** — no manual `dotnet ef database update` at runtime.

**Error handling / resilience:** DB connection retried 5× with 5s delays on startup. `PreventOverlapping("SyncJobs")` guards concurrent scraping. Graceful degradation when Python runtime unavailable (health check optional). Empty/default DB password triggers a startup warning. Ingestion ends with `VACUUM (VERBOSE, ANALYZE)`.

**State management:** `ImportMetadata` is a JSON document in Postgres storing `DmmLastImport`/`ImdbLastImport`. Resumable DMM sync picks up where interrupted. `ParsedPages` tracks ingestion progress to trigger first-run sync.

**Python interop:** RTN parsing runs in-process via pythonnet 3.0.4 against an embedded CPython 3.12. `PythonRuntimeService` (singleton) owns the engine lifecycle (`InitializePythonEngine`, `StopPythonEngine`, GIL handling). `TorrentParser` (singleton) owns the RTN parse orchestration + embedded script (`ParseAndPopulateAsync` — sync `foreach` over `parse_torrent_single` since Tier 6, replacing the prior asyncio-under-GIL path which added event-loop overhead with no concurrency benefit; `ParseAndPopulateTorrentInfoAsync`). `CategoryClassifier` (static) owns category detection (`DetectCategory`). Runtime expects `ZILEAN_PYTHON_PYLIB=/usr/lib/libpython3.12.so.1.0` (or `PYTHONNET_PYDLL`); pip packages vendored into project-local `python/` dirs. `ParserConcurrency.ResolveMaxConcurrentTasks` still exists (caps at `min(8, ProcessorCount)`) but is no longer called by the batch parse path — retained for future use + covered by `ParserConcurrencyTests`.

**Logging:** Serilog in the scraper; ASP.NET Core `ILogger<T>` in the API. Test logging captured via hand-rolled `CapturingLogger : ILogger` or `NSubstitute` substitutes; `LogBatchStageTimings` records per-stage durations.

## Important Files

| File | Role |
|------|------|
| `Zilean.sln` | Solution binding all projects; solution folders `src`, `eng`, `tests`, `scripts`, `k6`, `github`, `Internal Apps`. |
| `src/Zilean.ApiService/Program.cs` | API host entry point — DI wiring + endpoint mapping. |
| `src/Zilean.ApiService/Features/` | Endpoint groups (`Dmm`, `Imdb`, `Torznab`, `Torrents`, `Blacklist`, `HealthChecks`). |
| `src/Zilean.Scraper/Program.cs` | Scraper CLI entry point — Spectre command registration. |
| `src/Zilean.Scraper/Features/Commands/` | `dmm-sync`, `generic-sync`, `resync-imdb` commands. |
| `src/Zilean.Database/ZileanDbContext.cs` | EF Core context — DbSets for Torrents/ImdbFiles/ParsedPages/ImportMetadata/BlacklistedItems. |
| `src/Zilean.Database/ZileanDbContextDesignTimeFactory.cs` | Design-time factory for `dotnet ef migrations`. |
| `src/Zilean.Database/ModelConfiguration/TorrentInfoConfiguration.cs` | ~50-column mapping + trigram (GiST KNN) + GIN indexes. |
| `src/Zilean.Database/Functions/` | Raw SQL: `search_torrents_meta` (V7), `search_imdb_meta` (V3). |
| `src/Zilean.Database/Migrations/` | EF Core migrations (custom-prefixed via `eng/create-new-migration.sh`). |
| `src/Zilean.Database/Services/TorrentsQueryService.cs` | `/torrents/all` streaming (`StreamAllAsync` — `TryParse` on leading digits for non-numeric `Size`) + `/torrents/checkcached` (`CheckCachedAsync`). |
| `src/Zilean.Shared/Features/Configuration/` | `ZileanConfiguration`, `DatabaseConfiguration` (env-var resolution order: full connection string → `POSTGRES_*` individuals → defaults). |
| `src/Zilean.Shared/Features/Dmm/` | DMM DTOs + decompression (`Decompressor.FromEncodedUriComponent`). |
| `Directory.Build.props` | Central `TargetFramework=net9.0`, `Nullable=enable`, `ImplicitUsings=enable`, `LangVersion=latest`, central package management. |
| `Directory.Packages.props` | Central package version pinning (all `<PackageReference>` omit `Version`). |
| `Dockerfile` | Multi-stage Alpine build (`sdk:9.0.316-alpine3.23` → `aspnet:9.0.18-alpine3.23`) embedding Python 3.12. |
| `.editorconfig` | Formatting + analyzer severity rules (the real lint gate). |
| `.github/workflows/cicd.yaml` | Build, test (with CI filter), publish multi-arch image to ghcr.io. |
| `eng/create-new-migration.sh` | Migration scaffolding helper. |
| `eng/compose-dev.yaml` | Dev PostgreSQL 17.1 container. |

## Runtime/Tooling Preferences

- **.NET SDK 9.0.x** (`net9.0`, `LangVersion=latest`). No `global.json` pins the SDK; CI uses `setup-dotnet` `9.0.x`. Renovate does not manage the SDK version.
- **Package manager: NuGet with Central Package Management** (`ManagePackageVersionsCentrally=true`). All versions pinned in `Directory.Packages.props`; projects reference packages via `<PackageReference Include="..." />` without `Version`. `nuget.config` uses `packageSourceMapping` to force every package to `nuget.org`.
- **Database: PostgreSQL 16+** (CI test image `postgres:16.3-alpine3.20`; dev compose uses `postgres:17.1` — intentional skew to be aware of). Npgsql 9.0.0 + EFCore 9.0.0. Requires `pg_trgm` + `unaccent` extensions and `shm_size: 256m` for bulk upserts.
- **Container base:** `mcr.microsoft.com/dotnet/sdk:9.0.316-alpine3.23` (build) and `aspnet:9.0.18-alpine3.23` (runtime), Alpine 3.23, `python3.12` + pip for pythonnet interop.
- **Default runtime port: 8181** (`ASPNETCORE_URLS=http://+:8181`).
- **Python 3.12** embedded via pythonnet 3.0.4. Runtime expects `ZILEAN_PYTHON_PYLIB=/usr/lib/libpython3.12.so.1.0` (or `PYTHONNET_PYDLL`); pip packages installed into `/app/python`.
- **Release scheme:** NOT release-please in practice. `release-please-config.json` exists but no workflow invokes it. Actual releases use `mathieudutour/github-tag-action@v6.2` with `default_bump=patch` on main pushes, producing a `v` tag + GitHub Release. Conventional Commits enforced on PR **commits** via `webiny/action-conventional-commits` (validates each PR commit message, not the title). `docs.yaml` does **not** auto-run from the bot-created tag (GitHub suppresses `GITHUB_TOKEN`-generated push events); it is `workflow_dispatch`-only.
- **Renovate:** enabled, groups minor+patch, labels major as breaking; does not automerge.

## Testing & QA

**Frameworks** (versions from `Directory.Packages.props`):
- xUnit 2.9.2 + `xunit.runner.visualstudio` 2.8.2, `Microsoft.NET.Test.Sdk` 17.11.1
- **FluentAssertions** 6.12.2 — dominant style: `value.Should().Be(x, "because ...")`; messages explain the invariant under test
- **NSubstitute** 5.3.0 — for substituting `ILogger<T>` etc.
- Verify.Xunit 28.3.2 — `DerivePathInfo` routes snapshots to `tests/Zilean.Tests/Verification/` (wired, no `[Fact]` currently calls `.Verify()`)
- coverlet.collector 6.0.2 — `dotnet test --collect:"XPlat Code Coverage"`
- Microsoft.AspNetCore.Mvc.Testing 9.0.0 — `WebApplicationFactory<Program>` in-process hosting
- **Testcontainers.PostgreSql** 4.0.0 — ephemeral `postgres:16.3-alpine3.20` container
- Target framework `net9.0` (set centrally in `Directory.Build.props`)

**Project structure** (`tests/Zilean.Tests/`):
- `GlobalUsings.cs` — project-wide usings (xUnit, FluentAssertions, NSubstitute, Testcontainers, `Microsoft.AspNetCore.Mvc.Testing`, EF Core, `Zilean.Database`, `Zilean.Tests.Fixtures`, `Zilean.Tests.Collections`, plus `Zilean.Shared`/`Zilean.Scraper` feature namespaces). New test files generally don't need explicit `using` statements for these.
- `Fixtures/` — `PostgresLifecycleFixture` (shared container + factory), `ZileanWebApplicationFactory` (in-process API host), `TestDataBuilder` (static seed data).
- `Collections/ElasticTestCollectionDefinition.cs` — defines `ApiTestCollection` (legacy filename from the ES era) as `ICollectionFixture<PostgresLifecycleFixture>`.
- `Collections/SerializedEnvVarCollection.cs` — defines `SerializedEnvVarCollection` (`DisableParallelization = true`, also `ICollectionFixture<PostgresLifecycleFixture>`) for tests that mutate the process-global `ZILEAN_PYTHON_PYLIB` env var. `TorznabQueryValidationTests` (mutates static `TorznabCapabilities` lists) is in `ApiTestCollection` instead, to serialize with `ApiTestCollection` tests that read those same static lists.
- `xunit.runner.json` — `parallelizeAssembly=false`, `parallelizeTestCollections=false`; disables all test parallelization so env-var/static-state mutations cannot interleave.
- `Tests/` — 24 files; mix of integration (need the collection/fixture) and unit (no fixture).

**What is tested:**
- **Integration (HTTP + real Postgres):** `ApiIntegrationTests`, `ImdbMatchingServiceIdempotenceTests`, `ImdbMatcherThroughputTests`, `ApiKeyHeaderAuthenticationTests`, `DashboardAuthTests`, `BlacklistEndpointsTests`, `TorznabErrorTests`, `TorrentsEndpointsTests` — all `[Collection(nameof(ApiTestCollection))]`. Hit `/healthchecks/*`, `/torznab/api` (caps/search/movie/book/audiobook + trailing-year extraction + error 900/201 + DB-down), `/dmm/filtered` + `/dmm/search` (DB-down graceful degradation), `/torrents/checkcached` + `/torrents/all` (cached/uncached/stream); exercise `ImdbLuceneMatchingService` against seeded data. `TorznabQueryValidationTests` (unit, `[Collection(ApiTestCollection)]` for static caps serialization) mutates `MovieSearchParams` to prove capability-off throws/returns-false.
- **Unit (no DB):** `TorznabTests` (caps/query/RSS XML), `CategoryDetectionTests` (`CategoryClassifier.DetectCategory` static), `ConfigurationTests` (config binding + `DatabaseConfiguration` env-var logic), `SnapshotStalenessTests`, `StageTimingLoggerTests` (custom `CapturingLogger`), `StoreResultTests`, `ParserConcurrencyTests`, `ParsingExtractTrailingYearTests`, `MatcherLoggerLevelTests`, `IngestionPipelineTests` (producer URL/header construction per `GenericEndpointType` + exception swallowing via fake HTTP handler, `SerializedEnvVarCollection`), `PythonUnavailableTests` (deterministic `IsAvailable==false` on empty `ZILEAN_PYTHON_PYLIB`, `SerializedEnvVarCollection`).
- **Python-runtime:** `PttPythonTests`, `ParserParallelismTests` — `[Trait("Category","RequiresPython")]`, `[Collection(nameof(SerializedEnvVarCollection))]`. Require a real CPython 3.12 shared library on the host (`ZILEAN_PYTHON_PYLIB` / `PYTHONNET_PYDLL` → `libpython3.12`). `ParserParallelismTests` also carries `[Trait("Category","Benchmark")]`. `PythonUnavailableHealthCheckTests` (integration, `SerializedEnvVarCollection`) asserts `/healthchecks/ready` returns 200 `degraded` when Python is unavailable.
- **Performance-regression:** `ImdbMatcherThroughputTests` (`[Trait("Category","Benchmark")]`) uses `Stopwatch` to assert hoisted Lucene populate is ≥2× faster than per-batch (not BenchmarkDotNet).

**Database strategy:** a single shared `PostgreSqlContainer` per test run. `PostgresLifecycleFixture` implements `IAsyncLifetime` — starts the container, builds `ZileanWebApplicationFactory`, calls `Factory.CreateClient()` to block until the ASP.NET host fully starts (which runs EF Core migrations via the real `StartupService`), then seeds 5 rows via `TestDataBuilder.SeedAsync` and runs `ANALYZE "Torrents"` so pg_trgm similarity returns correct results on the small seed set. **No in-memory EF provider**; tests run against real Postgres 16.3. `ZileanWebApplicationFactory` sets environment `"Testing"`, injects in-memory config disabling scraping/ingestion and the dashboard while enabling the dmm/torznab/torrents/imdb endpoints, and removes only `ConfigurationUpdaterService` (writes config files to disk) so migrations still run. The container is shared across all `ApiTestCollection` tests (xUnit collection fixtures serialize by collection).

**Seed data:** `TestDataBuilder.SeedAsync` inserts 5 `TorrentInfo` rows — The Matrix (movie, `tt0133093`), The Witcher S01E01 (tv), Breaking Bad S05E16 (tv), Mistborn EPUB (book), Dune M4B (audiobook). Canonical set across integration tests.

**Test naming:** PascalCase for unit/integration tests (`HealthCheck_Ping_ReturnsPong`, `IsStale_ReturnsFalse_WhenSnapshotIsFresh`) but snake_case in `ConfigurationTests` (`adds_json_configuration_file_to_builder...`). Async tests return `Task` and are awaited. `[Theory]`/`[InlineData]` used heavily for parameterized cases.

**Coverage expectations:** no hard threshold. CI runs the filtered suite (`Category!=RequiresPython&Category!=Benchmark`); full suite requires Docker (Testcontainers) and a local Python 3.12 for trait tests. Test parallelization is disabled assembly-wide via `xunit.runner.json` (`parallelizeAssembly=false`) to prevent races on process-global state (`ZILEAN_PYTHON_PYLIB`, static `TorznabCapabilities` lists); the CI-filtered suite takes ~10 min (serialized) vs ~5 min (previously parallelized).

**Benchmarks** (`src/Zilean.Benchmarks`):
- BenchmarkDotNet 0.14.0. `Program.cs` is one line: `BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);` — discovers all `[Benchmark]` methods.
- `PythonParsing.cs` measures `TorrentParser.ParseAndPopulateAsync` throughput at 1k / 5k / 10k / 100k synthetic torrents (`[GlobalSetup]` sets both `ZILEAN_PYTHON_PYLIB` and `PYTHONNET_PYDLL` to a homebrew `libpython3.12.dylib` so `InitializePythonEngine` succeeds on a clean process).
- Run: `dotnet run --project src/Zilean.Benchmarks -c Release` (BenchmarkDotNet requires Release). References `Zilean.Scraper` (not the API) and copies `python/**` to the output directory.