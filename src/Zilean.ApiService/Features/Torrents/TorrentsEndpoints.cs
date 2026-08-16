namespace Zilean.ApiService.Features.Torrents;

/// <summary>
/// Provides extension methods for mapping torrents endpoints.
/// </summary>
public static class TorrentsEndpoints
{
    private const string GroupName = "torrents";
    private const string Scrape = "/all";
    private const string CheckCached = "/checkcached";
    private const string Lookup = "/{infoHash}";
    private const string NoHashesProvidedError = "No hashes provided";
    private const string TooManyHashesError = "Too many hashes provided. The limit is {0}.";

    /// <summary>
    /// Maps the torrents endpoints (<c>/all</c>, <c>/checkcached</c>), gated by
    /// <c>configuration.Torrents.EnableEndpoint</c> and requiring API key authorization.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="configuration">The Zilean configuration.</param>
    /// <returns>The web application with endpoints mapped.</returns>
    public static WebApplication MapTorrentsEndpoints(this WebApplication app, ZileanConfiguration configuration)
    {
        if (configuration.Torrents.EnableEndpoint)
        {
            app.MapGroup(GroupName)
                .WithTags(GroupName)
                .Torrents(configuration)
                .DisableAntiforgery()
                .RequireAuthorization(ApiKeyAuthentication.Policy)
                .WithMetadata(new OpenApiSecurityMetadata(ApiKeyAuthentication.Scheme));
        }

        return app;
    }

    private static RouteGroupBuilder Torrents(this RouteGroupBuilder group, ZileanConfiguration configuration)
    {
        if (configuration.Torrents.EnableScrapeEndpoint)
        {
            group.MapGet(Scrape, StreamTorrents)
                .Produces<StreamedEntry[]>();
        }

        if (configuration.Torrents.EnableCacheCheckEndpoint)
        {
            group.MapGet(CheckCached, CheckCachedTorrents)
                .Produces<CachedItem[]>()
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .Produces<BadRequest<ErrorResponse>>();
        }

        group.MapGet(Lookup, GetTorrentByHash)
            .Produces<TorrentInfo>()
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
        return group;
    }

    private static async Task<IResult> CheckCachedTorrents(HttpContext context, ITorrentsQueryService torrentsQueryService, ILogger<CheckCachedRequest> logger, ZileanConfiguration configuration, [AsParameters] CheckCachedRequest request)
    {
        try
        {
            if (request.Hashes.IsNullOrWhiteSpace())
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.BadRequest(new ErrorResponse(NoHashesProvidedError));
            }

            var hashes = request.Hashes.Split(',');

            if (hashes.Length > configuration.Torrents.MaxHashesToCheck)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.BadRequest(new ErrorResponse(string.Format(TooManyHashesError, configuration.Torrents.MaxHashesToCheck)));
            }

            var items = await torrentsQueryService.CheckCachedAsync(hashes, configuration.Torrents.MaxHashesToCheck, context.RequestAborted);
            return Results.Ok(items);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while checking for cached availability");
            return Results.Problem(ex.Message);
        }
    }

    private static async Task StreamTorrents(HttpContext context, ITorrentsQueryService torrentsQueryService, ILogger<StreamLogger> logger)
    {
        var sw = Stopwatch.StartNew();
        logger.LogInformation("Starting to stream torrents to client: {Client}", context.Connection.RemoteIpAddress);

        try
        {
            var response = context.Response;
            response.ContentType = "application/json";
            await using var writer = new Utf8JsonWriter(response.Body);

            await response.Body.WriteAsync("["u8.ToArray());

            var firstItem = true;

            await foreach (var item in torrentsQueryService.StreamAllAsync(context.RequestAborted))
            {
                if (!firstItem)
                {
                    await response.Body.WriteAsync(","u8.ToArray());
                }

                firstItem = false;

                await JsonSerializer.SerializeAsync(response.Body, item);
                await writer.FlushAsync();
            }

            await response.Body.WriteAsync("]"u8.ToArray());

            logger.LogInformation("Finished streaming torrents to client: {Client} in {Elapsed}s",
                context.Connection.RemoteIpAddress, sw.Elapsed.TotalSeconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while streaming torrents to client: {Client}", context.Connection.RemoteIpAddress);
        }
    }

    private static async Task<IResult> GetTorrentByHash(string infoHash, ITorrentsQueryService torrentsQueryService, HttpContext context, ILogger<CheckCachedLogger> logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(infoHash))
            {
                return Results.BadRequest(new ErrorResponse("InfoHash is required"));
            }

            var torrent = await torrentsQueryService.GetByInfoHashAsync(infoHash, context.RequestAborted);

            return torrent is null
                ? Results.NotFound(new ErrorResponse($"Torrent with info hash '{infoHash}' not found."))
                : Results.Ok(torrent);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occurred while looking up torrent {InfoHash}", infoHash);
            return Results.Problem(e.Message);
        }
    }

    private abstract class StreamLogger;
    private abstract class CheckCachedLogger;
}