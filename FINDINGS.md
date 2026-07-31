# Findings

Multi-category audit of the Zilean codebase (2026-07-25/26). Status verified against current code on 2026-07-27.

**Summary**

| Category | Fixed | Open | Total |
|---|---|---|---|
| SecurityAudit | 3 | 4 | 7 |
| ArchitectureSmells | 0 | 7 | 7 |
| TestCoverageGaps | 0 | 7 | 7 |
| PerformanceDb | 0 | 6 | 6 |
| **Total** | **3** | **24** | **27** |

---

## Recommended Implementation Order

Ordered by risk reduction, dependency, and effort. Tiers can be done in parallel within themselves; each tier's prerequisites are satisfied by earlier tiers.

### Tier 1 — Security hardening (do first, blocks exploitation)

1. **Security Finding 5 — GITHUB_TOKEN in cleartext git URL** (MED): switch to `GIT_ASKPASS` / credential helper to stop leaking the token in `.git/config`, process args, and logs. Self-contained change in `DmmFileDownloader.cs`.
2. **Security Finding 3 — Secrets in plaintext `settings.json`** (MED): separate secrets from the exported settings — keep the API key and Postgres password out of the world-readable JSON volume file (redact the password before serialization, store the API key in env/secret store or a restricted-permission secret file that the app loads from there). Don't use `[JsonIgnore]` on `ApiKey` blindly, as that would stop the generated key from persisting across restarts.
3. **Security Finding 6 — Container runs as root** (MED): add a non-root `USER` + `chown` to the Dockerfile run stage. No code change; build/test only.
4. **Security Finding 7 — Hardcoded Syncfusion license** (LOW): move to env/config. Quick, unblocks rotation without rebuild.

### Tier 2 — Test harness foundation (unblocks all other test gaps)

5. **TestGap GAP 1 — X-API-KEY header middleware tests** (HIGH): add integration tests sending missing/wrong/correct `X-API-KEY` to `/blacklist`, `/torrents`, `/on-demand-scrape`. This is the prerequisite for GAP 2 and GAP 7, which both need authenticated requests.

### Tier 3 — High-impact correctness & performance fixes

6. **Perf Finding 1 — N+1 per-page DB writes** (HIGH): batch `AddParsedPage` calls into `AddPagesToIngestedAsync` in `DmmFileEntryProcessor`. Biggest ingestion throughput win; the batch method already exists, just unused.
7. **Arch Finding 2 — Coravel jobs manually `new`'d** (HIGH): resolve `DmmSyncJob` from the container in `StartupService` and `SearchEndpoints` instead of `new`-ing. Prevents silent DI bypass bugs and unblocks Finding 3's extraction.
8. **TestGap GAP 2 — Blacklist endpoint tests** (HIGH): exercise all branches + torrent-delete side effect. Now unblocked by GAP 1. Guards the takedown mechanism.
9. **TestGap GAP 3 — `Validate()` + fail-fast tests** (HIGH): pure unit tests, no DB needed. Guards against misconfigured cron/batch sizes/score ranges.

### Tier 4 — Medium fixes, safe once Tier 3 lands

10. **Perf Finding 4 — Tracked entities + O(n×m) on `CheckCachedTorrents`** (MED): add `AsNoTracking`, project only needed fields, use a `HashSet` for O(1) lookup. Surgical, read-only endpoint.
11. **Perf Finding 3 — Captive DbContext in `EnsureMigrated`** (MED): inject `IServiceScopeFactory`/`IDbContextFactory`. Small, isolated to scraper startup.
12. **Perf Finding 2 — Sync-over-async IMDb load** (MED): switch to `QueryAsync`/`QueryUnbufferedAsync`. Touches both Lucene + Fuzzy matchers.
13. **Arch Finding 7 — Rename `ConditionallyRegisterDmmJob`** (LOW): trivial rename + drop misleading conditional. No behavior change.

### Tier 5 — Larger refactors (higher risk, more design)

14. **Arch Finding 1 — Service-locator anti-pattern** (HIGH): replace `IServiceProvider` + `CreateAsyncScope` with constructor-injected `IDbContextFactory<ZileanDbContext>`. Touches ~20 call sites across `TorrentInfoService`/`ImdbFileService`/`DmmService`; do after Tier 3/4 so the codebase is otherwise stable.
15. **Arch Finding 4 — Business logic in endpoint classes** (MED): extract `IBlacklistService` (owns workflow + transaction) and a torrents query service. Enables reuse from dashboard + tests; should land with or after GAP 2.
16. **Arch Finding 5 — Singleton IMDb matchers bypass data layer** (MED): inject `IImdbFileService` instead of direct `NpgsqlConnection`. Coordinate with Perf Finding 2 (same files).
17. **Arch Finding 6 — Dual Dapper/EF in one service** (MED): establish a clear read/write boundary or consolidate. Best done after Finding 1 and Finding 5 settle the data-access pattern.
18. **Arch Finding 3 — God class `ParseTorrentNameService`** (MED): split into `PythonRuntimeService` + `TorrentParser` + `CategoryClassifier`. Large but mechanical; the `CategoryClassifier` extraction also unblocks TestGap GAP 5.

### Tier 6 — Remaining test gaps + perf tuning

19. **TestGap GAP 4 — Generic ingestion pipeline tests** (HIGH): URL/header construction per `EndpointType` + exception loop. Lands cleanly after Arch Finding 3/4 settle the ingestion structure.
20. **TestGap GAP 5 — Python-unavailable branch tests** (MED): unsets `ZILEAN_PYTHON_PYLIB`, asserts `IsAvailable == false`, hits `/healthchecks/ready`. Easier once `CategoryClassifier` is split out (Arch Finding 3).
21. **TestGap GAP 6 — Torznab/search error + DB-down tests** (MED): error 900/201 + fault-injection. Needs stable endpoints from Tier 3/5.
22. **TestGap GAP 7 — `/torrents/checkcached` + `/torrents/all` tests** (LOW): validation bounds + stream. Unblocked by GAP 1 (Tier 2); deferred because it also benefits from Perf Finding 4 landing first.
23. **Perf Finding 5 — GIL-bound asyncio parsing** (MED): replace with a sync for-loop or multi-interpreter subprocesses. Needs careful benchmarking; do last to avoid churn.
24. **Perf Finding 6 — Trigram search sorts before LIMIT** (MED): subquery `LIMIT` or partial index. Needs query-level benchmarking against real data; do last.


## SecurityAudit (3 fixed, 4 open)

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

### OPEN — Finding 3 (MED)

**Path:** `src/Zilean.ApiService/Features/Bootstrapping/ConfigurationUpdaterService.cs:26-34` + `src/Zilean.Shared/Features/Configuration/DatabaseConfiguration.cs:24` + `src/Zilean.Shared/Features/Configuration/ZileanConfiguration.cs:11`

**Description:** Secrets persisted in plaintext to `data/settings.json` on every startup. `ConfigurationUpdaterService` serializes the entire `ZileanConfiguration` object — whose public properties include `ApiKey` (the auth secret) and `Database.ConnectionString` (which embeds the Postgres password built from `POSTGRES_PASSWORD`, with no `[JsonIgnore]`). The file is written with default permissions to `/app/data/settings.json` (a mounted volume). The connection string already lives in the process/env (necessary), but duplicating it plus the API key to a world/default-readable JSON file on a persistent volume broadens the secret's exposure surface (volume snapshots, backups, host-side reads, container-to-container).

**Remediation:** Add `[JsonIgnore]` to `ApiKey` and `Database.ConnectionString` (or redact the password before serialization); write secrets only to protected/locked-down paths.

**Verified:** Still serializes full `configuration` object; `DatabaseConfiguration.cs:24` has no `[JsonIgnore]`.

---

### FIXED — Finding 4 (MED)

**Path:** `src/Zilean.ApiService/Features/Authentication/ApiKeyAuthenticationHandler.cs:18`

**Description:** Non-constant-time API key comparison. Line 18 compares `extractedApiKey != configuredApiKey` using the default `==` operator, which short-circuits on first byte mismatch. Against the network the timing delta is small and noisy, so practical remote timing attacks are difficult, but this is the sole authentication secret and the fix is trivial/zero-cost. Keys are 64 hex chars (`ApiKey.Generate()` = two Guids concatenated), so the keyspace is large.

**Remediation:** Use `CryptographicOperations.FixedTimeEquals(ReadOnlySpan<byte>, ReadOnlySpan<byte>)` on the UTF8 bytes of both keys, after a length check; also return the same error for missing-vs-mismatched to avoid distinguishing presence.

**Verified:** `ApiKeyAuthenticationHandler.cs:19-21` now uses `CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(...), ...)`.

---

### OPEN — Finding 5 (MED)

**Path:** `src/Zilean.Scraper/Features/Ingestion/Dmm/DmmFileDownloader.cs:67-69`

**Description:** `GITHUB_TOKEN` embedded in cleartext git remote URL. `GetRepoUrlWithAuth` builds `https://{token}@github.com/owner/repo.git` and passes it to git clone/pull. The token then sits in `.git/config` of the cloned repo at `data/repo/.git/config` on disk, and any git command that errors may surface the URL (and thus the token) in process args/logs/ps output. A leaked token grants read/write to the token owner's scoped repos.

**Remediation:** Use git's credential helper or the `GIT_ASKPASS`/SSH env mechanisms instead of embedding the token in the URL; or at minimum redact the token from any logged command and run `git -c credential.helper='!f() { echo password=$GITHUB_TOKEN; }; f'` to avoid persisting it in `.git/config`.

**Verified:** `DmmFileDownloader.cs:69` still does `RepoUrl.Replace("https://", $"https://{githubToken}@")`.

---

### OPEN — Finding 6 (MED)

**Path:** `Dockerfile` (run stage, no `USER` directive)

**Description:** Container runs as root. The run stage (`FROM mcr.microsoft.com/dotnet/aspnet:9.0.18-alpine3.23`) has no `USER` instruction, so the ENTRYPOINT `./zilean-api` executes as UID 0. A process compromise (e.g. via a parsing bug in the pythonnet/RTN path or a deserialization issue) grants root inside the container, escalating container-escape and host-mutation impact. The app only needs to bind 8181 (already >1024) and write to `/app/data`.

**Remediation:** Add a non-root user in the run stage, e.g. `RUN addgroup -S zilean && adduser -S -G zilean zilean`, then `USER zilean`, and ensure the `/app/data` VOLUME is chowned to that UID (add `RUN mkdir -p /app/data && chown -R zilean:zilean /app` before the `USER` line).

**Verified:** `Dockerfile` run stage (lines 13-40) has no `USER` directive.

---

### OPEN — Finding 7 (LOW)

**Path:** `src/Zilean.ApiService/Features/Bootstrapping/WebApplicationExtensions.cs:48`

**Description:** Hardcoded Syncfusion license key in source. `RegisterLicense` is called with a literal base64 key embedded directly in `WebApplicationExtensions.cs:48`, compiled into the image and visible to anyone with repo/digest access. A leaked key risks Syncfusion revocation/throttling and can't be rotated without a rebuild.

**Remediation:** Move the key to configuration/environment (e.g. `Zilean__SyncfusionLicense`), read via `ZileanConfiguration`, and `RegisterLicense(configuration.SyncfusionLicense)`; fall back to community/no-license behavior if unset.

**Verified:** `WebApplicationExtensions.cs:51` still has the literal base64 key inline.

---

## ArchitectureSmells (1 fixed, 6 open)

### OPEN — Finding 1 (HIGH)

**Path:** `src/Zilean.Database/Services/TorrentInfoService.cs:3,27-30,8-9,188-189,201-202`

**Description:** Service-locator anti-pattern across the data layer. `TorrentInfoService`, `ImdbFileService`, `DmmService` all inject `IServiceProvider` and call `serviceProvider.CreateAsyncScope()` + `GetRequiredService<ZileanDbContext>()` inside every method (e.g. `StoreTorrentInfo:27-30`, `VaccumTorrentsIndexes:8-9`, `GetExistingInfoHashesAsync:188-189`). This hides real dependencies from the constructor, defeats lifetime diagnostics, makes each method a mini composition root, and is duplicated ~20× across the layer. WHY IT MATTERS: lifetime bugs are invisible until production (e.g. a Singleton resolving a Scoped DbContext captures the root scope's context); tests cannot substitute the context per-call; every method re-pays the scope-creation cost.

**Remediation:** Inject `ZileanDbContext` (or `IDbContextFactory<ZileanDbContext>`) directly via constructor and let the DI scope handle lifetime.

**Verified:** `TorrentInfoService.cs:3` still injects `IServiceProvider`; lines 8-9, 27-30 still `CreateAsyncScope()` + `GetRequiredService<ZileanDbContext>()`.

---

### OPEN — Finding 2 (HIGH)

**Path:** `src/Zilean.ApiService/Features/Bootstrapping/StartupService.cs:103-105` + `src/Zilean.ApiService/Features/Search/SearchEndpoints.cs:61`

**Description:** DI-registered Coravel jobs are manually `new`'d, bypassing the container. `DmmSyncJob` and `GenericSyncJob` are registered via `services.AddTransient<DmmSyncJob>()` for Coravel scheduling, but `StartupService.StartedAsync` manually constructs `new DmmSyncJob(executionService, loggerFactory.CreateLogger<DmmSyncJob>(), dbContext)` and `SearchEndpoints.PerformOnDemandScrape` does the same. WHY IT MATTERS: any constructor change, decorator, logging interceptor, or cancellation wiring added via DI is silently skipped on these two paths; the manually-built instance also uses a DbContext from an ad-hoc scope whose lifetime is uncorrelated with Coravel's, risking disposal/tracking bugs.

**Remediation:** Resolve the jobs from the DI container (e.g. via `IServiceScopeFactory` + scope, or `IHostedService`-style activation) instead of `new`-ing them directly.

**Verified:** `StartupService.cs:103` still `new DmmSyncJob(...)`; `SearchEndpoints.cs:61` still `new DmmSyncJob(...)`.

---

### OPEN — Finding 3 (MED)

**Path:** `src/Zilean.Shared/Features/Python/ParseTorrentNameService.cs:1-318,356-399`

**Description:** God class: `ParseTorrentNameService` conflates four responsibilities in one 410-line class: (a) Python runtime lifecycle (`InitializePythonEngine`/`StopPythonEngine`, GIL handling, `_mainThreadState`), (b) an 84-line embedded RTN parser script constant, (c) batch + single-torrent parse orchestration with manual `PyObject` disposal (`ParseAndPopulateAsync`, `ParseAndPopulateTorrentInfoAsync`), and (d) static category-classification business rules (`DetectCategory` + `_bookExtensions`/`_audiobookKeywords`). It is registered as both `AddSingleton` (ApiService) and `AddSingleton` (Scraper). WHY IT MATTERS: the static `DetectCategory` rules are pure domain logic with no Python dependency yet are unreachable for unit testing without the engine; runtime hosting concerns are tangled with parsing.

**Remediation:** Split into separate classes: a `PythonRuntimeService` (lifecycle + GIL), a `TorrentParser` (orchestration + script), and a `CategoryClassifier` (static rules, pure, testable).

**Verified:** Still 410 lines; all four responsibilities still in one class.

---

### OPEN — Finding 4 (MED)

**Path:** `src/Zilean.ApiService/Features/Blacklist/BlacklistEndpoints.cs:48-86` + `src/Zilean.ApiService/Features/Torrents/TorrentsEndpoints.cs:51-143`

**Description:** Business logic leaks into static endpoint classes. `BlacklistEndpoints.AddBlacklistItem` inlines the full blacklist workflow directly against `ZileanDbContext`: validation, duplicate check, creating the `BlacklistedItem` entity, deleting the matching torrent, and `SaveChanges`. `TorrentsEndpoints.StreamTorrents`/`CheckCachedTorrents` similarly build queries and do hash-limit enforcement inside the endpoint. WHY IT MATTERS: this logic is untestable without spinning up the web host, cannot be reused (e.g. the dashboard deletes torrents but re-implements its own path via `DashboardDataAdapter.RemoveAsync`), and mixes HTTP concerns with persistence/transaction concerns. The blacklist add+torrent-delete is also not atomic (two `SaveChanges` calls).

**Remediation:** Extract a service (e.g. `IBlacklistService`) that owns the workflow + transaction; endpoints delegate to it.

**Verified:** `BlacklistEndpoints.cs` still inlines the workflow against `ZileanDbContext`.

---

### OPEN — Finding 5 (MED)

**Path:** `src/Zilean.Database/Services/Lucene/ImdbLuceneMatchingService.cs:22-58,243-301` + `src/Zilean.Database/Services/FuzzyString/ImdbFuzzyStringMatchingService.cs:11-41,254-276`

**Description:** Singleton IMDb matchers bypass the data-access layer and own mutable in-memory state. Both `IImdbMatchingService` implementations are registered `Singleton` and each holds large mutable fields (Lucene: `_imdbFilesIndex`, `_reader`, `_searcher`, `_imdbCache`, a `SemaphoreSlim`; Fuzzy: `_imdbTvFiles`, `_imdbMovieFiles`, `_imdbCache`). To populate, both open `new NpgsqlConnection(configuration.Database.ConnectionString)` directly and run raw SQL — duplicating the IMDb load query that `ImdbFileService` already exposes via `SearchForImdbIdAsync`. WHY IT MATTERS: a Singleton with heavy mutable state plus its own DB connection handling duplicates the data-access layer's responsibilities and resists substitution in tests.

**Remediation:** Inject `IImdbFileService` (or a repository) and load via the existing data-access path instead of opening a direct `NpgsqlConnection`.

**Verified:** `ImdbLuceneMatchingService.cs:243` + `ImdbFuzzyStringMatchingService.cs:254,276` still open `new NpgsqlConnection` directly (bypass data-access layer); now use async Dapper calls.

---

### OPEN — Finding 6 (MED)

**Path:** `src/Zilean.Database/Services/BaseDapperService.cs:1-40` + `src/Zilean.Database/Services/TorrentInfoService.cs:73-117`

**Description:** Two competing data-access abstractions (Dapper + EF Core DbContext) in the same service with no boundary. `TorrentInfoService` inherits `BaseDapperService` (which provides `ExecuteCommandAsync` over raw `NpgsqlConnection` + Dapper) AND separately resolves `ZileanDbContext` for bulk upserts (`BulkInsertOrUpdateAsync`) and EF queries (`GetExistingInfoHashesAsync`). `SearchForTorrentInfoFiltered` runs Dapper against the `search_torrents_meta` function while `StoreTorrentInfo` uses EF Core BulkExtensions on the same table. WHY IT MATTERS: reads via Dapper and writes via EF can disagree on mapping/concurrency (e.g. the Dapper `TorrentInfoResult` projection vs the EF `TorrentInfo` entity), tracking and change-detection semantics are inconsistent, and contributors must know both ORM paradigms to touch one service.

**Remediation:** Choose one data-access path per service, or document a clear boundary (e.g. all reads via Dapper, all writes via EF) with shared mapping.

**Verified:** `TorrentInfoService` still inherits `BaseDapperService` AND uses `ZileanDbContext`.

---

### FIXED — Finding 7 (LOW)

**Path:** `src/Zilean.ApiService/Features/Bootstrapping/ServiceCollectionExtensions.cs:18-36`

**Description:** `ConditionallyRegisterDmmJob` registers jobs unconditionally and naming/lifetime are misleading. The method is named `ConditionallyRegisterDmmJob` but always registers `DmmSyncJob`, `GenericSyncJob`, and `SyncOnDemandState` regardless of `configuration.Dmm.EnableScraping`/`Ingestion.EnableScraping` — the conditionality only happens later in `SetupScheduling` (which decides whether to schedule). WHY IT MATTERS: the name misleads maintainers into thinking the types are gated; transient `DmmSyncJob`/`GenericSyncJob` capture a scoped `ZileanDbContext` via constructor injection, and because they're resolved transiently by Coravel per invocation that is fine, but the manual `new` sites in Finding 2 pass a context from a different scope.

**Remediation:** Rename to `RegisterSyncJobs` and drop the misleading "conditional".

**Verified:** Renamed to `RegisterSyncJobs` (`ServiceCollectionExtensions.cs:22`); callsite `Program.cs:14` updated. No behavior change.

---

## TestCoverageGaps (0 fixed, 7 open)

### OPEN — GAP 1 (HIGH)

**Path:** `src/Zilean.ApiService/Features/Authentication/ApiKeyAuthenticationHandler.cs:13-30` + `src/Zilean.ApiService/Features/Bootstrapping/ServiceCollectionExtensions.cs:54-70`

**Description:** API-key auth middleware is entirely untested. `ApiKeyAuthenticationHandler.HandleAuthenticateAsync` has three branches: missing `X-API-KEY` header → `Fail('API Key was not provided')`; configured key empty OR mismatch → `Fail('Invalid API Key')`; match → Success ticket. No test references `ApiKey`, `X-API-KEY`, `RequiresAuthorization`, `401`, or `403`. Why it matters: `ApiKeyAuthentication.Policy` gates every mutating/protected endpoint — `/blacklist/add|remove`, `/torrents/all` and `/torrents/checkcached`, and `/dmm/on-demand-scrape`. A regression in the handler (e.g. constant-time compare replaced with `==`, empty-key handling flipped, header name changed) would silently open or close all protected endpoints.

**Note:** `DashboardAuthTests.cs` only covers the dashboard **cookie** login flow (`/auth/login`), NOT the `X-API-KEY` **header** middleware that gates API endpoints. Still open.

**Remediation:** Add integration tests that send `X-API-KEY` (missing/wrong/correct) to `/blacklist`, `/torrents`, `/on-demand-scrape` and assert 401/403/200.

**Verified:** No test sends `X-API-KEY` to protected endpoints. Still open.

---

### OPEN — GAP 2 (HIGH)

**Path:** `src/Zilean.ApiService/Features/Blacklist/BlacklistEndpoints.cs:34-104`

**Description:** Blacklist endpoints (`/blacklist/add` PUT, `/blacklist/remove` DELETE) have zero tests. `AddBlacklistItem` has four branches: empty `info_hash` → 400; empty `reason` → 400; already-blacklisted → 409; success → 204 (and a side effect of removing the matching `Torrents` row). `RemoveBlacklistItem` has: empty `infoHash` → 400; not found → 404; success → 204. None are exercised; `ApiIntegrationTests` covers only anonymous `/torznab`, `/dmm/filtered`, `/healthchecks` paths. Why it matters: blacklisting is the abuse/takedown mechanism — Add also deletes the torrent from the DB, so a bug silently leaves prohibited content searchable or, conversely, wipes entries on a duplicate. The 409 idempotence and the cascade delete are both load-bearing.

**Remediation:** Add integration tests (with auth from GAP 1) covering all branches + the torrent-delete side effect.

**Verified:** No `blacklist`/`Blacklist` references in tests.

---

### OPEN — GAP 3 (HIGH)

**Path:** `src/Zilean.Shared/Features/Configuration/ZileanConfiguration.cs:34-66` + `src/Zilean.ApiService/Features/Bootstrapping/StartupService.cs:30-44`

**Description:** `ZileanConfiguration.Validate()` and the `StartupService` fail-fast path are untested. `Validate()` checks `MaxFilteredResults>0`, `MinimumScoreMatch` in [0,1], `MinimumReDownloadIntervalMinutes>=0`, cron validity for Dmm+Ingestion schedules (5-space-part rule), `Parsing.BatchSize>0`, and non-empty `ConnectionString`; `StartupService.StartingAsync` throws `InvalidOperationException` when errors are non-empty. `ConfigurationTests` only exercises `DatabaseConfiguration` env-var binding and JSON deserialization — no call to `Validate()` or any 'Configuration error' assertion in the test tree. Why it matters: `Validate()` is the only guard against misconfigured cron schedules and negative batch sizes; an invalid `MinimumScoreMatch` or `BatchSize` could break search or ingestion silently.

**Remediation:** Add unit tests for `Validate()` covering each rule (valid + invalid) and the `StartupService` throw path.

**Verified:** No `Validate()` / "Configuration error" references in tests.

---

### OPEN — GAP 4 (HIGH)

**Path:** `src/Zilean.Scraper/Features/Ingestion/Processing/StreamedEntryProcessor.cs:30-87` + `src/Zilean.Scraper/Features/Ingestion/Endpoints/GenericIngestionScraping.cs:16-66`

**Description:** Generic ingestion pipeline (`StreamedEntryProcessor` + `GenericIngestionScraping`) has no tests at all. `ProduceEntriesAsync` builds the URL per `EndpointType` (Zurg/Zilean/Generic), sets `X-Api-Key` or `Authorization` headers, streams JSON via `DeserializeAsyncEnumerable`, and catches exceptions per-URL in the outer loop; the channel consumer in `GenericProcessor.OnProcessTorrentsAsync` dedupes, filters existing hashes, parses via Python, filters blacklisted, and stores. No test references `StreamedEntry`, `GenericIngestion`, `DmmScraping`, `KubernetesServiceDiscovery`, `DmmFileEntry`, or `Vaccum`. Why it matters: this is the primary ingestion path for zurg/zilean instances; the header logic (X-Api-Key only for Zilean endpoints, Authorization only for Generic) and URL-switching are untested.

**Remediation:** Add tests for URL/header construction per `EndpointType` and the exception-per-URL loop.

**Verified:** No `GenericIngestion`/`StreamedEntryProcessor`/`on-demand-scrape` references in tests.

---

### OPEN — GAP 5 (MED)

**Path:** `src/Zilean.Shared/Features/Python/ParseTorrentNameService.cs:356-410` + `src/Zilean.ApiService/Features/HealthChecks/HealthCheckEndpoints.cs:33-58`

**Description:** `ParseTorrentNameService` has no coverage outside the `RequiresPython`-tagged tests, and the Python-unavailable branch is entirely untested. `InitializePythonEngine` returns `Task.FromException` when `ZILEAN_PYTHON_PYLIB` is unset or when `PythonEngine.Initialize` throws; `IsAvailable` then reports false; the `/healthchecks/ready` endpoint surfaces `pythonAvailable=false` as 'degraded'. The only `ParseTorrentNameService` tests (`PttPythonTests`, `ParserParallelismTests`) are tagged `RequiresPython` and skip in CI; `CategoryDetectionTests` exercises only the static `DetectCategory` heuristic, not the Python interop. Why it matters: in any environment without the exact `libpython3.12` dylib, ingestion silently degrades and the health check's degraded state is untested.

**Remediation:** Add a test that unsets `ZILEAN_PYTHON_PYLIB`, constructs the service, asserts `IsAvailable == false`, and hits `/healthchecks/ready` expecting degraded.

**Verified:** Still no coverage outside `RequiresPython`-tagged tests.

---

### OPEN — GAP 6 (MED)

**Path:** `src/Zilean.ApiService/Features/Torznab/TorznabEndpoints.cs:42-66` + `src/Zilean.ApiService/Features/Search/SearchEndpoints.cs:57-95` + `src/Zilean.Database/Services/TorrentInfoService.cs:85-160`

**Description:** Torznab/search error and validation branches, plus the DB-down path, are untested. `TorznabEndpoints.ValidateAndPrepareQuery` returns error 900 for `limit>LimitsMax`, error 201 for unsupported query types, and `NewErrorResponse(900, ex.Message)` on any exception; `SearchEndpoints.PerformSearch`/`PerformFilteredSearch` swallow exceptions and return empty arrays. `TorrentInfoService.SearchForTorrentInfoFiltered` rethrows via `BaseDapperService.ExecuteCommandAsync` on a DB failure, so the DB-down behavior is endpoint-dependent: Torznab returns a 400 XML error, `/dmm/filtered` returns 200 with `[]`. `ApiIntegrationTests` only covers happy/empty-data paths against a healthy Postgres; no fault-injection.

**Remediation:** Add tests for error 900 (limit too high), error 201 (unsupported query type), and the DB-down path (stop Postgres / mock failure).

**Verified:** Still open.

---

### OPEN — GAP 7 (LOW, supplementary)

**Path:** `src/Zilean.ApiService/Features/Torrents/TorrentsEndpoints.cs:46-140`

**Description:** `/torrents/checkcached` and `/torrents/all` stream endpoints are untested. `CheckCachedTorrents` has `NoHashesProvided` (400), `TooManyHashes` (400, `MaxHashesToCheck`), and the cached/uncached result assembly; `StreamTorrents` writes a manual JSON array with cancellation support. Both require auth (covered by GAP 1) so they are blocked from testing until the API-key harness exists, but the validation bounds (empty hashes, over-limit) are cheap pure-ish tests once auth is in place. Why it matters: `checkcached` is the debrid-cache lookup used by zurg-like consumers; an off-by-one on `MaxHashesToCheck` or a broken JSON stream bracket would break cache checks silently.

**Remediation:** After GAP 1, add integration tests with auth: `GET /torrents/checkcached?hashes=` (400), over-limit (400), valid (200 with cached/uncached), and `GET /torrents/all` stream.

**Verified:** Still open (blocked on GAP 1).

---

## PerformanceDb (3 fixed, 3 open)

### OPEN — Finding 1 (HIGH)

**Path:** `src/Zilean.Scraper/Features/Ingestion/Processing/DmmFileEntryProcessor.cs:101,119,131` + `src/Zilean.Database/Services/DmmService.cs:46-52`

**Description:** N+1 per-page DB writes. `DmmFileEntryProcessor.ProcessPageAsync` calls `AddParsedPage` (→ `dmmService.AddPageToIngestedAsync`) once per file, on three code paths (no-match, empty, success, error). Each call spins its own service scope + DbContext and runs `SaveChangesAsync` individually. `DmmService` already exposes `AddPagesToIngestedAsync` (batch, line 38) but it is never called. Impact: a 10K-file DMM sync performs 10K separate INSERT round-trips.

**Remediation:** Collect `ParsedPages` in the `ProduceEntriesAsync` loop and call `AddPagesToIngestedAsync` once per batch (or once at end), or buffer pages and flush in chunks.

**Verified:** `DmmFileEntryProcessor.cs:101,119,131` still calls `AddParsedPage` per file; `AddPagesToIngestedAsync` (batch) exists at `DmmService.cs:38` but is never called.

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

### OPEN — Finding 5 (MED)

**Path:** `src/Zilean.Shared/Features/Python/ParseTorrentNameService.cs:147-151` + `src/Zilean.Shared/Features/Configuration/ParserConcurrency.cs:7-8`

**Description:** GIL-bound parsing with ineffective asyncio concurrency. The entire batch run executes inside a single `using (Py.GIL())` block (line 147); `run_process_batches` launches asyncio tasks gated by a `Semaphore(maxConcurrentTasks)`. RTN's `parse()` is CPU-bound pure Python, so the GIL serializes it regardless of the semaphore — the asyncio concurrency adds task/scheduling overhead without throughput gain. `ParserConcurrency` caps at `min(ProcessorCount, 8)`. Impact: on >8-core hosts the cap leaves cores idle; the asyncio overhead (task creation, semaphore, context switches) is pure cost for CPU-bound work.

**Remediation:** For CPU-bound parsing, a plain synchronous for-loop calling `parse_torrent_single` is as fast with less overhead; if real parallelism is needed, run multiple Python interpreters in subprocesses.

**Verified:** Still open.

---

### OPEN — Finding 6 (MED)

**Path:** `src/Zilean.Database/Functions/SearchTorrentsMetaV6.cs:88-95`

**Description:** Trigram search sorts all threshold matches before LIMIT. The V6 function filters with `t."CleanedParsedTitle" % query` (uses the `idx_cleaned_parsed_title_trgm` GIN index for the `%` predicate), then computes `similarity(...)` and `ORDER BY "Score" DESC` over ALL surviving rows, and only then applies `LIMIT limit_param`. The `ORDER BY` on a computed expression cannot use the index, so PostgreSQL materializes and fully sorts every row above the (possibly lowered) `effective_threshold` before discarding all but the top-N. With a low `effective_threshold` (0.85×0.3 ≈ 0.255 for short queries with filters) and a large Torrents table, the intermediate sort set can be large. Impact: high CPU/sort-memory cost and slow p95 for broad filtered queries.

**Remediation:** (a) Ensure the query uses a restrictive enough threshold, (b) consider a partial index or materialized view for common query patterns, or (c) use `LIMIT` in a subquery before the final sort.

**Verified:** `SearchTorrentsMetaV6.cs` unchanged.