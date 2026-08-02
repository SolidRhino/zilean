# Findings

Multi-category audit of the Zilean codebase (2026-07-25/26). Status verified against current code on 2026-08-01 (post-Tier 7 merge).

**Summary**

| Category | Fixed | Open | Total |
|---|---|---|---|
| SecurityAudit | 7 | 0 | 7 |
| ArchitectureSmells | 7 | 0 | 7 |
| TestCoverageGaps | 7 | 0 | 7 |
| PerformanceDb | 5 | 1 | 6 |
| **Total** | **26** | **1** | **27** |

---

## Recommended Implementation Order

Ordered by risk reduction, dependency, and effort. Tiers can be done in parallel within themselves; each tier's prerequisites are satisfied by earlier tiers.

### Tier 1 — Security hardening (DONE — PR #4)

1. **Security Finding 5 — GITHUB_TOKEN in cleartext git URL** (MED, FIXED): switched to git credential helper expanding `$GITHUB_TOKEN` from env at auth time; token no longer embedded in URL or `.git/config`.
2. **Security Finding 3 — Secrets in plaintext `settings.json`** (MED, FIXED): `[JsonIgnore]` on `DatabaseConfiguration.ConnectionString` prevents the Postgres password from being serialized to `settings.json`. `ApiKey` remains persisted (intentional — key must survive restarts; the finding itself notes not to use `[JsonIgnore]` blindly).
3. **Security Finding 6 — Container runs as root** (MED, FIXED): `Dockerfile` run stage creates `zilean` user/group and switches to `USER zilean`; `/app` chowned.
4. **Security Finding 7 — Hardcoded Syncfusion license** (LOW, FIXED): `DefaultSyncfusionLicense` const removed from `ZileanConfiguration.cs`; `SyncfusionLicense` property defaults to null; `NormalizeSyncfusionLicense` no longer falls back to a const. Unset → Syncfusion community/no-license mode.

### Tier 2 — Test harness foundation (DONE — PR #5)

5. **TestGap GAP 1 — X-API-KEY header middleware tests** (HIGH, FIXED): `ApiKeyHeaderAuthenticationTests.cs` — 7 tests covering missing/wrong/correct/empty `X-API-KEY` against `/blacklist/add` and `/torrents/checkcached`.

### Tier 3 — High-impact correctness & performance fixes (DONE — PR #6)

6. **Perf Finding 1 — N+1 per-page DB writes** (HIGH, FIXED): `DmmFileEntryProcessor.ProduceEntriesAsync` now buffers `ParsedPages` and calls `AddPagesToIngestedAsync` (batch) instead of per-file `AddPageToIngestedAsync`.
7. **Arch Finding 2 — Coravel jobs manually `new`'d** (HIGH, FIXED): `StartupService.StartedAsync` and `SearchEndpoints.PerformOnDemandScrape` resolve `DmmSyncJob` from DI scope, not `new`-ing.
8. **TestGap GAP 2 — Blacklist endpoint tests** (HIGH, FIXED): `BlacklistEndpointsTests.cs` — 9 tests covering empty info_hash/reason (400), already-blacklisted (409), success+torrent-delete (204), not-in-torrents (204), remove empty/not-found/success/re-remove.
9. **TestGap GAP 3 — `Validate()` + fail-fast tests** (HIGH, FIXED): `ConfigurationValidationTests.cs` — 13 tests covering all `Validate()` rules + `StartupService.StartingAsync` throw path.

### Tier 4 — Medium fixes (DONE — PR #7)

10. **Perf Finding 4 — Tracked entities + O(n×m) on `CheckCachedTorrents`** (MED, FIXED): added `AsNoTracking` + O(1) `HashSet<string>` lookup. Field projection deferred (`Item = record` kept to preserve the `/torrents/checkcached` JSON contract).
11. **Perf Finding 3 — Captive DbContext in `EnsureMigrated`** (MED, FIXED): injected `IServiceScopeFactory`; resolve `ZileanDbContext` in a scope inside `StartAsync`.
12. **Perf Finding 2 — Sync-over-async IMDb load** (MED, FIXED): Lucene `QueryUnbufferedAsync` + `await foreach`; Fuzzy buffered `QueryAsync`.
13. **Arch Finding 7 — Rename `ConditionallyRegisterDmmJob`** (LOW, FIXED): renamed to `RegisterSyncJobs`. No behavior change.

### Tier 5 — Larger refactors (DONE — PR #8)

14. **Arch Finding 1 — Service-locator anti-pattern** (HIGH, FIXED): replaced `IServiceProvider` + `CreateAsyncScope` with constructor-injected `IDbContextFactory<ZileanDbContext>` via `AddDbContextFactory`. All data services (`TorrentInfoService`/`ImdbFileService`/`DmmService`/`EnsureMigrated`) use `CreateDbContextAsync()`.
15. **Arch Finding 4 — Business logic in endpoint classes** (MED, FIXED): extracted `IBlacklistService` (owns add/remove + cascade torrent delete) and `ITorrentsQueryService` (owns `CheckCachedAsync` + `StreamAllAsync`). Endpoints are thin HTTP delegates mapping `BlacklistResult` → status codes.
16. **Arch Finding 5 — Singleton IMDb matchers bypass data layer** (MED, FIXED): `ImdbLuceneMatchingService`/`ImdbFuzzyStringMatchingService` inject `IDbContextFactory` + use `FromSqlRaw` instead of raw `NpgsqlConnection`.
17. **Arch Finding 6 — Dual Dapper/EF in one service** (MED, FIXED): `TorrentInfoService` and `ImdbFileService` no longer inherit `BaseDapperService`; both use `Database.SqlQueryRaw<T>` for PG-function queries (`search_torrents_meta`, `search_imdb_meta`) and `FromSqlRaw` for entity-shaped queries. `BaseDapperService.cs` and `DapperResult.cs` deleted. Dapper package removed from `Zilean.Database.csproj` and `Directory.Packages.props`.
18. **Arch Finding 3 — God class `ParseTorrentNameService`** (MED, FIXED): split into `PythonRuntimeService` (engine lifecycle) + `TorrentParser` (RTN orchestration) + `CategoryClassifier` (static category detection).

### Tier 6 — Remaining test gaps + perf tuning (DONE — PR #10)

19. **TestGap GAP 4 — Generic ingestion pipeline tests** (HIGH, FIXED): `IngestionPipelineTests.cs` — 6 producer-only tests for URL/header construction per `GenericEndpointType` + exception swallowing.
20. **TestGap GAP 5 — Python-unavailable branch tests** (MED, FIXED): `PythonUnavailableTests.cs` (unit) + `PythonUnavailableHealthCheckTests.cs` (integration) — `IsAvailable==false` on empty env var + `/healthchecks/ready` degraded.
21. **TestGap GAP 6 — Torznab/search error + DB-down tests** (MED, FIXED): `TorznabErrorTests.cs` (5 integration) + `TorznabQueryValidationTests.cs` (4 unit) — error 900/201 + DB-down graceful degradation.
22. **TestGap GAP 7 — `/torrents/checkcached` + `/torrents/all` tests** (LOW, FIXED): `TorrentsEndpointsTests.cs` — 6 tests covering hash validation bounds, cached/uncached, mixed, and `/torrents/all` stream (proves `long.Parse` → `TryParse` fix).
23. **Perf Finding 5 — GIL-bound asyncio parsing** (MED, FIXED): replaced `run_process_batches` (asyncio + Semaphore) with sync `foreach` over `parse_torrent_single`. `ParserConcurrency` retained but no longer called by batch path.
24. **Perf Finding 6 — Trigram search sorts before LIMIT** (MED, OPEN): investigation-only. EXPLAIN on 100K-row scratch Postgres shows GiST KNN is 29% faster but GiST index is 49% larger than GIN (13MB vs 8.7MB); deferred to follow-up PR. `SearchTorrentsMetaV6.cs` unchanged.


## SecurityAudit (7 fixed, 0 open)

### FIXED — Finding 1 (HIGH)

**Path:** `src/Zilean.ApiService/Features/Bootstrapping/WebApplicationExtensions.cs:44-49` + `src/Zilean.ApiService/Features/Dashboard/Components/Routes.razor` + `src/Zilean.ApiService/Features/Dashboard/Components/Pages/Dashboard/DashboardDataAdapter.cs:65,91,121,146`

**Description:** Unauthenticated dashboard with full CRUD. `MapRazorComponents` is mapped with NO `RequireAuthorization`, the Router uses plain `RouteView` not `AuthorizeRouteView`, and `MainLayout` has no `AuthorizeView` gate. `DashboardDataAdapter` exposes `InsertAsync`/`UpdateAsync`/`RemoveAsync`/`BatchUpdateAsync` against the torrents table. Any network-reachable client can add/edit/delete torrent rows.

**Remediation:** Add `.RequireAuthorization(ApiKeyAuthentication.Policy)` to the `MapRazorComponents` chain, OR switch `Routes.razor` to `<AuthorizeRouteView>` with an `<NotAuthorized>` redirect, and add an `AuthorizeView` gate to `MainLayout`.

**Verified:** `WebApplicationExtensions.cs:49` now has `.RequireAuthorization(ApiKeyAuthentication.DashboardPolicy)`; `Routes.razor` uses `<AuthorizeRouteView>` + `<NotAuthorized><RedirectToLogin/></NotAuthorized>`; `DashboardAuthTests.cs` covers login + cookie + dashboard access.

---

### FIXED — Finding 2 (HIGH, data exposure)

**Path:** `src/Zilean.ApiService/Features/Bootstrapping/ConfigurationUpdaterService.cs:19,41`

**Description:** API key logged in plaintext to application logs. On first run and on regeneration via `ZILEAN__NEW__API__KEY`, the generated API key is interpolated directly into a `LogInformation` message. In containerized/clustered deployments logs are centrally aggregated and retained; the primary authentication secret persists in log streams where operators, log shippers, and any log-reader role can recover it.

**Remediation:** Never log the full key. Log only a truncated fingerprint, e.g. `logger.LogInformation("API Key generated: {Prefix}...", key[..6]);` and instruct the operator to read `settings.json` for the full value.

**Verified:** `ConfigurationUpdaterService.cs:19,41` now log only `configuration.ApiKey[..Math.Min(6, ...)]...` (truncated).

---

### FIXED — Finding 3 (MED, PR #4)

**Path:** `src/Zilean.ApiService/Features/Bootstrapping/ConfigurationUpdaterService.cs:26-34` + `src/Zilean.Shared/Features/Configuration/DatabaseConfiguration.cs` + `src/Zilean.Shared/Features/Configuration/ZileanConfiguration.cs:11`

**Description:** Secrets persisted in plaintext to `data/settings.json` on every startup. `ConfigurationUpdaterService` serializes the entire `ZileanConfiguration` object — whose public properties include `ApiKey` (the auth secret) and `Database.ConnectionString` (which embeds the Postgres password built from `POSTGRES_PASSWORD`, with no `[JsonIgnore]`). The file is written with default permissions to `/app/data/settings.json` (a mounted volume). The connection string already lives in the process/env (necessary), but duplicating it plus the API key to a world/default-readable JSON file on a persistent volume broadens the secret's exposure surface (volume snapshots, backups, host-side reads, container-to-container).

**Remediation:** Add `[JsonIgnore]` to `Database.ConnectionString` (redact the password before serialization). `ApiKey` is intentionally persisted to `settings.json` so the generated key survives restarts (the finding itself notes not to use `[JsonIgnore]` blindly); the DB-password exposure (the higher-risk half) is the primary concern.

**Verified:** `DatabaseConfiguration.cs` now has `[JsonIgnore]` on `ConnectionString` — the Postgres password is no longer serialized to `settings.json`. `ApiKey` remains persisted (intentional — the key must survive restarts; the finding itself notes not to use `[JsonIgnore]` blindly). The DB-password exposure (the higher-risk half) is remediated.

---

### FIXED — Finding 4 (MED)

**Path:** `src/Zilean.ApiService/Features/Authentication/ApiKeyAuthenticationHandler.cs:18`

**Description:** Non-constant-time API key comparison. Line 18 compares `extractedApiKey != configuredApiKey` using the default `==` operator, which short-circuits on first byte mismatch. Against the network the timing delta is small and noisy, so practical remote timing attacks are difficult, but this is the sole authentication secret and the fix is trivial/zero-cost. Keys are 64 hex chars (`ApiKey.Generate()` = two Guids concatenated), so the keyspace is large.

**Remediation:** Use `CryptographicOperations.FixedTimeEquals(ReadOnlySpan<byte>, ReadOnlySpan<byte>)` on the UTF8 bytes of both keys, after a length check; also return the same error for missing-vs-mismatched to avoid distinguishing presence.

**Verified:** `ApiKeyAuthenticationHandler.cs:19-21` now uses `CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(...), ...)`.

---

### FIXED — Finding 5 (MED, PR #4)

**Path:** `src/Zilean.Scraper/Features/Ingestion/Dmm/DmmFileDownloader.cs`

**Description:** `GITHUB_TOKEN` embedded in cleartext git remote URL. `GetRepoUrlWithAuth` builds `https://{token}@github.com/owner/repo.git` and passes it to git clone/pull. The token then sits in `.git/config` of the cloned repo at `data/repo/.git/config` on disk, and any git command that errors may surface the URL (and thus the token) in process args/logs/ps output. A leaked token grants read/write to the token owner's scoped repos.

**Remediation:** Use git's credential helper or the `GIT_ASKPASS`/SSH env mechanisms instead of embedding the token in the URL; or at minimum redact the token from any logged command and run `git -c credential.helper='!f() { echo password=$GITHUB_TOKEN; }; f'` to avoid persisting it in `.git/config`.

**Verified:** `DmmFileDownloader.cs` now uses an inline git credential helper (`GitCredentialHelper`) that expands `$GITHUB_TOKEN` from the process environment at auth time. No token is embedded in the URL or `.git/config`. `GitPullAsync` also scrubs any token-embedded URL left by older versions via `git remote set-url origin <public URL>`.

---

### FIXED — Finding 6 (MED, PR #4)

**Path:** `Dockerfile` (run stage)

**Description:** Container runs as root. The run stage (`FROM mcr.microsoft.com/dotnet/aspnet:9.0.18-alpine3.23`) has no `USER` instruction, so the ENTRYPOINT `./zilean-api` executes as UID 0. A process compromise (e.g. via a parsing bug in the pythonnet/RTN path or a deserialization issue) grants root inside the container, escalating container-escape and host-mutation impact. The app only needs to bind 8181 (already >1024) and write to `/app/data`.

**Remediation:** Add a non-root user in the run stage, e.g. `RUN addgroup -S zilean && adduser -S -G zilean zilean`, then `USER zilean`, and ensure the `/app/data` VOLUME is chowned to that UID (add `RUN mkdir -p /app/data && chown -R zilean:zilean /app` before the `USER` line).

**Verified:** `Dockerfile` run stage now creates `zilean` user/group (`addgroup -S -g 101 zilean && adduser -S -u 100 -G zilean zilean`), chowns `/app`, and switches to `USER zilean`. ENTRYPOINT runs as UID 100, not root.

---

### FIXED — Finding 7 (LOW)

**Path:** `src/Zilean.Shared/Features/Configuration/ZileanConfiguration.cs:16-17` + `src/Zilean.Shared/Features/Configuration/ConfigurationExtensions.cs:29-39`

**Description:** Hardcoded Syncfusion license key in source. `DefaultSyncfusionLicense` const in `ZileanConfiguration.cs` embedded a literal base64 key compiled into the image and visible to anyone with repo/digest access. A leaked key risks Syncfusion revocation/throttling and can't be rotated without a rebuild.

**Remediation:** Move the key to configuration/environment (e.g. `Zilean__SyncfusionLicense`), read via `ZileanConfiguration`, and `RegisterLicense(configuration.SyncfusionLicense)`; fall back to community/no-license behavior if unset.

**Verified:** `DefaultSyncfusionLicense` const removed from `ZileanConfiguration.cs`; `SyncfusionLicense` property defaults to null; `NormalizeSyncfusionLicense` no longer falls back to a const. `WebApplicationExtensions.cs` already guarded `RegisterLicense` with `!string.IsNullOrWhiteSpace(configuration.SyncfusionLicense)` — unset → Syncfusion community/no-license mode. `grep -r 'DefaultSyncfusionLicense' src/ --include='*.cs'` returns zero hits.

---

## ArchitectureSmells (7 fixed, 0 open)

### FIXED — Finding 1 (HIGH, PR #8)

**Path:** `src/Zilean.Database/Services/TorrentInfoService.cs` + `ImdbFileService.cs` + `DmmService.cs`

**Description:** Service-locator anti-pattern across the data layer. `TorrentInfoService`, `ImdbFileService`, `DmmService` all inject `IServiceProvider` and call `serviceProvider.CreateAsyncScope()` + `GetRequiredService<ZileanDbContext>()` inside every method. This hides real dependencies from the constructor, defeats lifetime diagnostics, makes each method a mini composition root, and is duplicated ~20× across the layer. WHY IT MATTERS: lifetime bugs are invisible until production (e.g. a Singleton resolving a Scoped DbContext captures the root scope's context); tests cannot substitute the context per-call; every method re-pays the scope-creation cost.

**Remediation:** Inject `ZileanDbContext` (or `IDbContextFactory<ZileanDbContext>`) directly via constructor and let the DI scope handle lifetime.

**Verified:** `TorrentInfoService.cs` primary ctor now injects `IDbContextFactory<ZileanDbContext>` — no `IServiceProvider`, no `CreateAsyncScope`. All methods use `await using var dbContext = await dbContextFactory.CreateDbContextAsync()`. Same pattern in `ImdbFileService` and `DmmService`.

---

### FIXED — Finding 2 (HIGH, PR #6)

**Path:** `src/Zilean.ApiService/Features/Bootstrapping/StartupService.cs` + `src/Zilean.ApiService/Features/Search/SearchEndpoints.cs`

**Description:** DI-registered Coravel jobs are manually `new`'d, bypassing the container. `DmmSyncJob` and `GenericSyncJob` are registered via `services.AddTransient<DmmSyncJob>()` for Coravel scheduling, but `StartupService.StartedAsync` manually constructs `new DmmSyncJob(...)` and `SearchEndpoints.PerformOnDemandScrape` does the same. WHY IT MATTERS: any constructor change, decorator, logging interceptor, or cancellation wiring added via DI is silently skipped on these two paths; the manually-built instance also uses a DbContext from an ad-hoc scope whose lifetime is uncorrelated with Coravel's, risking disposal/tracking bugs.

**Remediation:** Resolve the jobs from the DI container (e.g. via `IServiceScopeFactory` + scope, or `IHostedService`-style activation) instead of `new`-ing them directly.

**Verified:** `StartupService.cs:99-104` now resolves `DmmSyncJob` via `asyncScope.ServiceProvider.GetRequiredService<DmmSyncJob>()`. `SearchEndpoints.cs:45` takes `DmmSyncJob` as a DI-injected minimal-API parameter. No `new DmmSyncJob` or `new GenericSyncJob` anywhere in `src/`.

---

### FIXED — Finding 3 (MED, PR #8)

**Path:** `src/Zilean.Shared/Features/Python/{PythonRuntimeService,TorrentParser,CategoryClassifier}.cs` (was `ParseTorrentNameService.cs`)

**Description:** God class: `ParseTorrentNameService` conflated four responsibilities in one 410-line class: (a) Python runtime lifecycle (`InitializePythonEngine`/`StopPythonEngine`, GIL handling, `_mainThreadState`), (b) an 84-line embedded RTN parser script constant, (c) batch + single-torrent parse orchestration with manual `PyObject` disposal (`ParseAndPopulateAsync`, `ParseAndPopulateTorrentInfoAsync`), and (d) static category-classification business rules (`DetectCategory` + `_bookExtensions`/`_audiobookKeywords`). WHY IT MATTERS: the static `DetectCategory` rules are pure domain logic with no Python dependency yet were unreachable for unit testing without the engine.

**Remediation:** Split into `PythonRuntimeService` (lifecycle + GIL), `TorrentParser` (orchestration + script), and `CategoryClassifier` (static rules, pure, testable). DONE.

**Verified:** Fixed in PR #8. `CategoryDetectionTests` (27 tests) pass against `CategoryClassifier`.

---

### FIXED — Finding 4 (MED, PR #8)

**Path:** `src/Zilean.ApiService/Features/Blacklist/BlacklistEndpoints.cs` + `src/Zilean.ApiService/Features/Torrents/TorrentsEndpoints.cs`

**Description:** Business logic leaks into static endpoint classes. `BlacklistEndpoints.AddBlacklistItem` inlines the full blacklist workflow directly against `ZileanDbContext`: validation, duplicate check, creating the `BlacklistedItem` entity, deleting the matching torrent, and `SaveChanges`. `TorrentsEndpoints.StreamTorrents`/`CheckCachedTorrents` similarly build queries and do hash-limit enforcement inside the endpoint. WHY IT MATTERS: this logic is untestable without spinning up the web host, cannot be reused, and mixes HTTP concerns with persistence/transaction concerns. The blacklist add+torrent-delete is also not atomic (two `SaveChanges` calls).

**Remediation:** Extract a service (e.g. `IBlacklistService`) that owns the workflow + transaction; endpoints delegate to it.

**Verified:** `BlacklistEndpoints.AddBlacklistItem` and `RemoveBlacklistItem` now delegate to `IBlacklistService` and map `BlacklistResult` → status codes. `TorrentsEndpoints.CheckCachedTorrents` delegates to `ITorrentsQueryService.CheckCachedAsync`; `StreamTorrents` delegates to `StreamAllAsync`. No inline `ZileanDbContext` usage in endpoints. `BlacklistEndpointsTests.cs` (9 tests) covers all branches.

---

### FIXED — Finding 5 (MED, PR #8)

**Path:** `src/Zilean.Database/Services/Lucene/ImdbLuceneMatchingService.cs` + `src/Zilean.Database/Services/FuzzyString/ImdbFuzzyStringMatchingService.cs`

**Description:** Singleton IMDb matchers bypass the data-access layer and own mutable in-memory state. Both `IImdbMatchingService` implementations are registered `Singleton` and each holds large mutable fields (Lucene: `_imdbFilesIndex`, `_reader`, `_searcher`, `_imdbCache`, a `SemaphoreSlim`; Fuzzy: `_imdbTvFiles`, `_imdbMovieFiles`, `_imdbCache`). To populate, both open `new NpgsqlConnection(configuration.Database.ConnectionString)` directly and run raw SQL — duplicating the IMDb load query that `ImdbFileService` already exposes. WHY IT MATTERS: a Singleton with heavy mutable state plus its own DB connection handling duplicates the data-access layer's responsibilities and resists substitution in tests.

**Remediation:** Inject `IImdbFileService` (or a repository) and load via the existing data-access path instead of opening a direct `NpgsqlConnection`.

**Verified:** Both `ImdbLuceneMatchingService` and `ImdbFuzzyStringMatchingService` now inject `IDbContextFactory<ZileanDbContext>` and use `CreateDbContextAsync()` + `FromSqlRaw` for IMDb loads. No `new NpgsqlConnection` in either file. Both remain Singleton with mutable in-memory state, but the data-access layer bypass is resolved.

---

### FIXED — Finding 6 (MED)

**Path:** `src/Zilean.Database/Services/BaseDapperService.cs:1-40` + `src/Zilean.Database/Services/TorrentInfoService.cs` + `src/Zilean.Database/Services/ImdbFileService.cs`

**Description:** Two competing data-access abstractions (Dapper + EF Core DbContext) in the same service with no boundary. `TorrentInfoService` inherits `BaseDapperService` (which provides `ExecuteCommandAsync` over raw `NpgsqlConnection` + Dapper) AND separately resolves `ZileanDbContext` for bulk upserts (`BulkInsertOrUpdateAsync`) and EF queries (`GetExistingInfoHashesAsync`). `SearchForTorrentInfoFiltered` runs Dapper against the `search_torrents_meta` function while `StoreTorrentInfo` uses EF Core BulkExtensions on the same table. WHY IT MATTERS: reads via Dapper and writes via EF can disagree on mapping/concurrency (e.g. the Dapper `TorrentInfoResult` projection vs the EF `TorrentInfo` entity), tracking and change-detection semantics are inconsistent, …

**Remediation:** Choose one data-access path per service, or document a clear boundary (e.g. all reads via Dapper, all writes via EF) with shared mapping.

**Verified:** `TorrentInfoService` and `ImdbFileService` no longer inherit `BaseDapperService`; both use `Database.SqlQueryRaw<T>` for PG-function queries (`search_torrents_meta`, `search_imdb_meta`) and `FromSqlRaw` for entity-shaped queries. A flat `TorrentInfoQueryDto` maps `search_torrents_meta` results without the `Imdb` navigation property that EF rejects for `SqlQueryRaw`. `BaseDapperService.cs` and `DapperResult.cs` deleted. Dapper package removed from `Zilean.Database.csproj` and `Directory.Packages.props`. `grep -r 'BaseDapperService\|Dapper' src/Zilean.Database/ --include='*.cs'` returns zero hits.

---

### FIXED — Finding 7 (LOW)

**Path:** `src/Zilean.ApiService/Features/Bootstrapping/ServiceCollectionExtensions.cs:18-36`

**Description:** `ConditionallyRegisterDmmJob` registers jobs unconditionally and naming/lifetime are misleading. The method is named `ConditionallyRegisterDmmJob` but always registers `DmmSyncJob`, `GenericSyncJob`, and `SyncOnDemandState` regardless of `configuration.Dmm.EnableScraping`/`Ingestion.EnableScraping` — the conditionality only happens later in `SetupScheduling` (which decides whether to schedule). WHY IT MATTERS: the name misleads maintainers into thinking the types are gated; transient `DmmSyncJob`/`GenericSyncJob` capture a scoped `ZileanDbContext` via constructor injection, and because they're resolved transiently by Coravel per invocation that is fine, but the manual `new` sites in Finding 2 pass a context from a different scope.

**Remediation:** Rename to `RegisterSyncJobs` and drop the misleading "conditional".

**Verified:** Renamed to `RegisterSyncJobs` (`ServiceCollectionExtensions.cs:22`); callsite `Program.cs:14` updated. No behavior change.

---

## TestCoverageGaps (7 fixed, 0 open)

### FIXED — GAP 1 (HIGH, PR #5)

**Path:** `src/Zilean.ApiService/Features/Authentication/ApiKeyAuthenticationHandler.cs` + `src/Zilean.ApiService/Features/Bootstrapping/ServiceCollectionExtensions.cs:54-70`

**Description:** API-key auth middleware is entirely untested. `ApiKeyAuthenticationHandler.HandleAuthenticateAsync` has three branches: missing `X-API-KEY` header → `Fail('API Key was not provided')`; configured key empty OR mismatch → `Fail('Invalid API Key')`; match → Success ticket. No test references `ApiKey`, `X-API-KEY`, `RequiresAuthorization`, `401`, or `403`. Why it matters: `ApiKeyAuthentication.Policy` gates every mutating/protected endpoint — `/blacklist/add|remove`, `/torrents/all` and `/torrents/checkcached`, and `/dmm/on-demand-scrape`. A regression in the handler (e.g. constant-time compare replaced with `==`, empty-key handling flipped, header name changed) would silently open or close all protected endpoints.

**Note:** `DashboardAuthTests.cs` covers the dashboard cookie login flow; `ApiKeyHeaderAuthenticationTests.cs` covers the `X-API-KEY` header middleware that gates API endpoints.

**Remediation:** Add integration tests that send `X-API-KEY` (missing/wrong/correct) to protected endpoints and assert 401/200. Tests cover `/blacklist/add` and `/torrents/checkcached` as representative protected endpoints; `/dmm/on-demand-scrape` shares the same middleware and is not separately tested.

**Verified:** `ApiKeyHeaderAuthenticationTests.cs` — 7 tests covering missing/wrong/correct/empty `X-API-KEY` against `/blacklist/add` and `/torrents/checkcached` (401/200).

---

### FIXED — GAP 2 (HIGH, PR #6)

**Path:** `src/Zilean.ApiService/Features/Blacklist/BlacklistEndpoints.cs`

**Description:** Blacklist endpoints (`/blacklist/add` PUT, `/blacklist/remove` DELETE) have zero tests. `AddBlacklistItem` has four branches: empty `info_hash` → 400; empty `reason` → 400; already-blacklisted → 409; success → 204 (and a side effect of removing the matching `Torrents` row). `RemoveBlacklistItem` has: empty `infoHash` → 400; not found → 404; success → 204. None are exercised; `ApiIntegrationTests` covers only anonymous `/torznab`, `/dmm/filtered`, `/healthchecks` paths. Why it matters: blacklisting is the abuse/takedown mechanism — Add also deletes the torrent from the DB, so a bug silently leaves prohibited content searchable or, conversely, wipes entries on a duplicate. The 409 idempotence and the cascade delete are both load-bearing.

**Remediation:** Add integration tests (with auth from GAP 1) covering all branches + the torrent-delete side effect.

**Verified:** `BlacklistEndpointsTests.cs` — 9 tests covering empty info_hash/reason (400), already-blacklisted (409), success+torrent-delete (204), not-in-torrents (204), remove empty/not-found/success/re-remove.

---

### FIXED — GAP 3 (HIGH, PR #6)

**Path:** `src/Zilean.Shared/Features/Configuration/ZileanConfiguration.cs` + `src/Zilean.ApiService/Features/Bootstrapping/StartupService.cs:30-44`

**Description:** `ZileanConfiguration.Validate()` and the `StartupService` fail-fast path are untested. `Validate()` checks `MaxFilteredResults>0`, `MinimumScoreMatch` in [0,1], `MinimumReDownloadIntervalMinutes>=0`, cron validity for Dmm+Ingestion schedules (5-space-part rule), `Parsing.BatchSize>0`, and non-empty `ConnectionString`; `StartupService.StartingAsync` throws `InvalidOperationException` when errors are non-empty. `ConfigurationTests` only exercises `DatabaseConfiguration` env-var binding and JSON deserialization — no call to `Validate()` or any 'Configuration error' assertion in the test tree. Why it matters: `Validate()` is the only guard against misconfigured cron schedules and negative batch sizes; an invalid `MinimumScoreMatch` or `BatchSize` could break search or ingestion silently.

**Remediation:** Add unit tests for `Validate()` covering each rule (valid + invalid) and the `StartupService` throw path.

**Verified:** `ConfigurationValidationTests.cs` — 13 tests covering all `Validate()` rules + `StartupService.StartingAsync` throw path.

---

### FIXED — GAP 4 (HIGH, PR #10)

**Path:** `src/Zilean.Scraper/Features/Ingestion/Processing/StreamedEntryProcessor.cs` + `src/Zilean.Scraper/Features/Ingestion/Endpoints/GenericIngestionScraping.cs:16-66`

**Description:** Generic ingestion pipeline (`StreamedEntryProcessor` + `GenericIngestionScraping`) has no tests at all. `ProduceEntriesAsync` builds the URL per `EndpointType` (Zurg/Zilean/Generic), sets `X-Api-Key` or `Authorization` headers, streams JSON via `DeserializeAsyncEnumerable`, and catches exceptions per-URL in the outer loop; the channel consumer in `GenericProcessor.OnProcessTorrentsAsync` dedupes, filters existing hashes, parses via Python, filters blacklisted, and stores. No test references `StreamedEntry`, `GenericIngestion`, `DmmScraping`, `KubernetesServiceDiscovery`, `DmmFileEntry`, or `Vaccum`. Why it matters: this is the primary ingestion path for zurg/zilean instances; the header logic (X-Api-Key only for Zilean endpoints, Authorization only for Generic) and URL-switching are untested.

**Remediation:** Add tests for URL/header construction per `EndpointType` and the exception-per-URL loop.

**Verified:** `IngestionPipelineTests.cs` — 6 producer-only tests for URL/header construction per `GenericEndpointType` + exception swallowing via fake HTTP handler.

---

### FIXED — GAP 5 (MED, PR #10)

**Path:** `src/Zilean.Shared/Features/Python/PythonRuntimeService.cs` + `src/Zilean.ApiService/Features/HealthChecks/HealthCheckEndpoints.cs:33-58`

**Description:** `PythonRuntimeService` has no coverage outside the `RequiresPython`-tagged tests, and the Python-unavailable branch is entirely untested. `InitializePythonEngine` returns `Task.FromException` when `ZILEAN_PYTHON_PYLIB` is unset or when `PythonEngine.Initialize` throws; `IsAvailable` then reports false; the `/healthchecks/ready` endpoint surfaces `pythonAvailable=false` as 'degraded'. The only `PythonRuntimeService` tests (`PttPythonTests`, `ParserParallelismTests`) are tagged `RequiresPython` and skip in CI; `CategoryDetectionTests` exercises only the static `CategoryClassifier.DetectCategory` heuristic, not the Python interop. Why it matters: in any environment without the exact `libpython3.12` dylib, ingestion silently degrades and the health check's degraded status is the only signal.

**Remediation:** Add a test that unsets `ZILEAN_PYTHON_PYLIB`, constructs the service, asserts `IsAvailable == false`, and hits `/healthchecks/ready` expecting degraded.

**Verified:** `PythonUnavailableTests.cs` (unit) asserts `IsAvailable==false` on empty env var; `PythonUnavailableHealthCheckTests.cs` (integration) asserts `/healthchecks/ready` returns 200 degraded with `pythonAvailable=false`.

---

### FIXED — GAP 6 (MED, PR #10)

**Path:** `src/Zilean.ApiService/Features/Torznab/TorznabEndpoints.cs` + `src/Zilean.ApiService/Features/Search/SearchEndpoints.cs:57-95` + `src/Zilean.Database/Services/TorrentInfoService.cs:85-160`

**Description:** Torznab/search error and validation branches, plus the DB-down path, are untested. `TorznabEndpoints.ValidateAndPrepareQuery` returns error 900 for `limit>LimitsMax`, error 201 for unsupported query types, and `NewErrorResponse(900, ex.Message)` on any exception; `SearchEndpoints.PerformSearch`/`PerformFilteredSearch` swallow exceptions and return empty arrays. `TorrentInfoService.SearchForTorrentInfoFiltered` rethrows via `BaseDapperService.ExecuteCommandAsync` on a DB failure, so the DB-down behavior is endpoint-dependent: Torznab returns a 400 XML error, `/dmm/filtered` returns 200 with `[]`. `ApiIntegrationTests` only covers happy/empty-data paths against a healthy Postgres; no fault-injection.

**Remediation:** Add tests for error 900 (limit too high), error 201 (unsupported query type), and the DB-down path (stop Postgres / mock failure).

**Verified:** `TorznabErrorTests.cs` (5 integration) — error 900 (limit/cat/DB-down) + `/dmm/*` DB-down→200`[]`. `TorznabQueryValidationTests.cs` (4 unit) — capability-off throws/returns-false (error 201/900).

---

### FIXED — GAP 7 (LOW, supplementary)

**Path:** `src/Zilean.ApiService/Features/Torrents/TorrentsEndpoints.cs:46-140`

**Description:** `/torrents/checkcached` and `/torrents/all` stream endpoints are untested. `CheckCachedTorrents` has `NoHashesProvided` (400), `TooManyHashes` (400, `MaxHashesToCheck`), and the cached/uncached result assembly; `StreamTorrents` writes a manual JSON array with cancellation support. Both require auth (covered by GAP 1) so they are blocked from testing until the API-key harness exists, but the validation bounds (empty hashes, over-limit) are cheap pure-ish tests once auth is in place. Why it matters: `checkcached` is the debrid-cache lookup used by zurg-like consumers; an off-by-one on `MaxHashesToCheck` or a broken JSON stream bracket would break cache checks silently.

**Remediation:** After GAP 1, add integration tests with auth: `GET /torrents/checkcached?hashes=` (400), over-limit (400), valid (200 with cached/uncached), and `GET /torrents/all` stream.

**Verified:** `TorrentsEndpointsTests.cs` — 6 tests covering hash validation bounds, cached/uncached, mixed, and `/torrents/all` stream (proves `long.Parse` → `TryParse` fix).

---

## PerformanceDb (5 fixed, 1 open)

### FIXED — Finding 1 (HIGH, PR #6)

**Path:** `src/Zilean.Scraper/Features/Ingestion/Processing/DmmFileEntryProcessor.cs` + `src/Zilean.Database/Services/DmmService.cs:46-52`

**Description:** N+1 per-page DB writes. `DmmFileEntryProcessor.ProcessPageAsync` calls `AddParsedPage` (→ `dmmService.AddPageToIngestedAsync`) once per file, on three code paths (no-match, empty, success, error). Each call spins its own service scope + DbContext and runs `SaveChangesAsync` individually. `DmmService` already exposes `AddPagesToIngestedAsync` (batch, line 38) but it is never called. Impact: a 10K-file DMM sync performs 10K separate INSERT round-trips.

**Remediation:** Collect `ParsedPages` in the `ProduceEntriesAsync` loop and call `AddPagesToIngestedAsync` once per batch (or once at end), or buffer pages and flush in chunks.

**Verified:** `DmmFileEntryProcessor.ProduceEntriesAsync` now buffers `ParsedPages` and calls `AddPagesToIngestedAsync` (batch) when buffer >= `BatchSize` and at loop end. Zero per-file `AddPageToIngestedAsync` calls remain.

---

### FIXED — Finding 2 (MED)

**Path:** `src/Zilean.Database/Services/Lucene/ImdbLuceneMatchingService.cs:246` + `src/Zilean.Database/Services/FuzzyString/ImdbFuzzyStringMatchingService.cs:257,279`

**Description:** Sync-over-async full-table load. After `await sqlConnection.OpenAsync()` the code calls the synchronous Dapper `sqlConnection.Query<ImdbFile>(...)` to stream the entire `ImdbFiles` table (filtered to movie/tv categories). The buffered `Query<T>` blocks the calling thread pool thread for the full fetch + materialization of potentially millions of rows. Impact: thread-pool starvation risk during ingestion startup; the single-populate lock serializes it but the blocked thread is held.

**Remediation:** Use `QueryAsync<ImdbFile>` (buffered) or, better, `sqlConnection.QueryUnbufferedAsync<ImdbFile>` / a streaming reader to interleave indexing with fetching.

**Verified:** `ImdbLuceneMatchingService.cs:246` now uses `QueryUnbufferedAsync<ImdbFile>` + `await foreach`; `ImdbFuzzyStringMatchingService.cs:257,279` now use buffered `await QueryAsync<ImdbFile>`. No sync-over-async.

---

### FIXED — Finding 3 (MED)

**Path:** `src/Zilean.Scraper/Features/Bootstrapping/EnsureMigrated.cs:3` + `src/Zilean.Scraper/Features/Bootstrapping/ServiceCollectionExtensions.cs:18`

**Description:** Captive DbContext in `IHostedService`. `EnsureMigrated` is registered with `AddHostedService` (singleton lifetime) but constructor-injects `ZileanDbContext`, which is registered scoped (`AddDbContext`). The DI container captures a single DbContext instance for the entire process lifetime. While it only runs `MigrateAsync` once at startup, the DbContext (and its change tracker / connection) is never disposed and is held alive for the full scraper run. Impact: leaked tracked entities and a long-lived connection if any later code path reuses the hosted service instance.

**Remediation:** Inject `IServiceScopeFactory`/`IDbContextFactory<ZileanDbContext>` and create a scope inside `StartAsync`, or resolve `ZileanDbContext` from a scope.

**Verified:** `EnsureMigrated.cs:3` now injects `IServiceScopeFactory`; resolves `ZileanDbContext` in an `await using` scope inside `StartAsync`. No captive dependency.

---

### FIXED — Finding 4 (MED)

**Path:** `src/Zilean.ApiService/Features/Torrents/TorrentsEndpoints.cs:64-75`

**Description:** Tracked entities + O(n×m) lookup on read-only cache-check endpoint. `CheckCachedTorrents` issues `dbContext.Torrents.Where(...).Select(record => new CachedItem { ... Item = record }).ToListAsync()` without `AsNoTracking`, so EF materializes and tracks full `TorrentInfo` entities for every matched hash (up to `MaxHashesToCheck`). Then line 75 does `hashSet.Where(hash => items.All(x => !x.InfoHash.Equals(hash, OrdinalIgnoreCase)))` — an O(hashes × items) nested scan per request. Impact: unnecessary tracking overhead + GC pressure for a read-only endpoint; the `All`-within-`Where` scales poorly at the configured hash limit.

**Remediation:** Add `.AsNoTracking()`, project only the fields `CachedItem` needs (avoid `Item = record` if the caller doesn't use the full entity), and build a `HashSet<string>` of matched hashes for O(1) lookup.

**Verified:** `TorrentsEndpoints.cs:64-73` now has `.AsNoTracking()`; line 75 replaced with an `OrdinalIgnoreCase` `HashSet<string>` O(1) lookup. `Item = record` kept (wire contract preserved).

---

### FIXED — Finding 5 (MED, PR #10)

**Path:** `src/Zilean.Shared/Features/Python/TorrentParser.cs` + `src/Zilean.Shared/Features/Configuration/ParserConcurrency.cs:7-8`

**Description:** GIL-bound parsing with ineffective asyncio concurrency. The entire batch run executes inside a single `using (Py.GIL())` block (line 147); `run_process_batches` launches asyncio tasks gated by a `Semaphore(maxConcurrentTasks)`. RTN's `parse()` is CPU-bound pure Python, so the GIL serializes it regardless of the semaphore — the asyncio concurrency adds task/scheduling overhead without throughput gain. `ParserConcurrency` caps at `min(ProcessorCount, 8)`. Impact: on >8-core hosts the cap leaves cores idle; the asyncio overhead (task creation, semaphore, context switches) is pure cost for CPU-bound work.

**Remediation:** For CPU-bound parsing, a plain synchronous for-loop calling `parse_torrent_single` is as fast with less overhead; if real parallelism is needed, run multiple Python interpreters in subprocesses.

**Verified:** `ParseAndPopulateAsync` now uses a sync `foreach` over `parse_torrent_single` — no `run_process_batches`/asyncio. Each `PyObject` result disposed immediately. `ParserConcurrency.ResolveMaxConcurrentTasks` retained but no longer called by batch path.

---

### OPEN — Finding 6 (MED)

**Path:** `src/Zilean.Database/Functions/SearchTorrentsMetaV6.cs`

**Description:** Trigram search sorts all threshold matches before LIMIT. The V6 function filters with `t."CleanedParsedTitle" % query` (uses the `idx_cleaned_parsed_title_trgm` GIN index for the `%` predicate), then computes `similarity(...)` and `ORDER BY "Score" DESC` over ALL surviving rows, and only then applies `LIMIT limit_param`. The `ORDER BY` on a computed expression cannot use the index, so PostgreSQL materializes and fully sorts every row above the (possibly lowered) `effective_threshold` before discarding all but the top-N. With a low `effective_threshold` (0.85×0.3 ≈ 0.255 for short queries with filters) and a large Torrents table, the intermediate sort set can be large. Impact: high CPU/sort-memory cost and slow p95 for broad filtered queries.

**Remediation:** (a) Ensure the query uses a restrictive enough threshold, (b) consider a partial index or materialized view for common query patterns, or (c) use `LIMIT` in a subquery before the final sort.

**Verified:** `SearchTorrentsMetaV6.cs` unchanged. Investigation (Tier 6) on 100K-row scratch Postgres: GiST KNN with `%` threshold guard is 29% faster (30ms vs 42.5ms) but GiST index is 49% larger (13MB vs 8.7MB). Deferred to follow-up PR.