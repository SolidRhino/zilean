using Npgsql;
using Zilean.Shared.Features.Statistics;

namespace Zilean.ApiService.Features.Status;

/// <summary>
/// Provides extension methods for mapping the status endpoint.
/// </summary>
public static class StatusEndpoints
{
    private const string GroupName = "status";

    /// <summary>
    /// Maps the status endpoint (<c>/status</c>), exposing operational metadata (database health,
    /// counts, last import timestamps, sync state, Python runtime availability). Anonymous, like
    /// the health check endpoints — exposes no secrets.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application with the status endpoint mapped.</returns>
    public static WebApplication MapStatusEndpoints(this WebApplication app)
    {
        app.MapGroup(GroupName)
            .WithTags(GroupName)
            .Status()
            .DisableAntiforgery()
            .AllowAnonymous();

        return app;
    }

    private static RouteGroupBuilder Status(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetStatus);
        return group;
    }

    private static async Task<IResult> GetStatus(
        ZileanConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<StatusLogger> logger)
    {
        var databaseHealthy = false;

        try
        {
            await using var connection = new NpgsqlConnection(configuration.Database.ConnectionString);
            await connection.OpenAsync();
            databaseHealthy = true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database health check failed");
        }

        long torrentCount = 0;
        long imdbFileCount = 0;
        long blacklistedCount = 0;
        long parsedPagesCount = 0;
        DmmLastImport? dmmLastImport = null;
        ImdbLastImport? imdbLastImport = null;
        bool isSyncRunning = false;
        bool? pythonAvailable = null;

        if (databaseHealthy)
        {
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<ZileanDbContext>>();
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            torrentCount = await dbContext.Torrents.AsNoTracking().LongCountAsync();
            imdbFileCount = await dbContext.ImdbFiles.AsNoTracking().LongCountAsync();
            blacklistedCount = await dbContext.BlacklistedItems.AsNoTracking().LongCountAsync();
            parsedPagesCount = await dbContext.ParsedPages.AsNoTracking().LongCountAsync();

            var dmmService = serviceProvider.GetRequiredService<DmmService>();
            dmmLastImport = await dmmService.GetDmmLastImportAsync(default);

            var imdbFileService = serviceProvider.GetRequiredService<IImdbFileService>();
            imdbLastImport = await imdbFileService.GetImdbLastImportAsync(default);

            var syncState = serviceProvider.GetService<SyncOnDemandState>();
            isSyncRunning = syncState?.IsRunning ?? false;
        }

        if (configuration.EnableDashboard)
        {
            var ptn = serviceProvider.GetService<PythonRuntimeService>();
            pythonAvailable = ptn?.IsAvailable ?? false;
        }

        var status = !databaseHealthy ? "unhealthy" : pythonAvailable == false ? "degraded" : "healthy";
        var statusCode = databaseHealthy ? 200 : 503;

        return Results.Json(new
        {
            status,
            database = databaseHealthy,
            pythonAvailable,
            isSyncRunning,
            timestamp = DateTime.UtcNow,
            counts = new
            {
                torrents = torrentCount,
                imdbFiles = imdbFileCount,
                blacklisted = blacklistedCount,
                parsedPages = parsedPagesCount,
            },
            dmmLastImport = dmmLastImport is null ? null : new
            {
                dmmLastImport.OccuredAt,
                dmmLastImport.Status,
                dmmLastImport.PageCount,
                dmmLastImport.EntryCount,
            },
            imdbLastImport = imdbLastImport is null ? null : new
            {
                imdbLastImport.OccuredAt,
                imdbLastImport.Status,
                imdbLastImport.EntryCount,
            },
        }, statusCode: statusCode);
    }

    private abstract class StatusLogger;
}