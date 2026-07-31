namespace Zilean.ApiService.Features.Blacklist;

public static class BlacklistEndpoints
{
    private const string GroupName = "blacklist";
    private const string Add = "/add";
    private const string Remove = "/remove";

    public static WebApplication MapBlacklistEndpoints(this WebApplication app)
    {
        app.MapGroup(GroupName)
            .WithTags(GroupName)
            .Torrents()
            .DisableAntiforgery()
            .RequireAuthorization(ApiKeyAuthentication.Policy)
            .WithMetadata(new OpenApiSecurityMetadata(ApiKeyAuthentication.Scheme));

        return app;
    }

    private static RouteGroupBuilder Torrents(this RouteGroupBuilder group)
    {
        group.MapPut(Add, AddBlacklistItem)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<string>(StatusCodes.Status400BadRequest)
            .Produces<string>(StatusCodes.Status409Conflict);

        group.MapDelete(Remove, RemoveBlacklistItem)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<string>(StatusCodes.Status404NotFound)
            .Produces<string>(StatusCodes.Status400BadRequest);

        return group;
    }

    private static async Task<IResult> RemoveBlacklistItem(HttpContext context, IBlacklistService blacklistService, ILogger<BlacklistLogger> logger, [FromQuery] string infoHash)
    {
        try
        {
            var result = await blacklistService.RemoveAsync(infoHash, context.RequestAborted);

            return result switch
            {
                BlacklistResult.InvalidHash => Results.BadRequest("InfoHash is required"),
                BlacklistResult.NotFound => Results.NotFound(),
                BlacklistResult.Added => Results.NoContent(),
                _ => Results.BadRequest("An error occurred while removing a blacklisted item")
            };
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occurred while removing a blacklisted item");
            return Results.BadRequest("An error occurred while removing a blacklisted item");
        }
    }

    private static async Task<IResult> AddBlacklistItem(HttpContext context, IBlacklistService blacklistService, [AsParameters] BlacklistItemRequest request, ILogger<BlacklistLogger> logger)
    {
        try
        {
            var result = await blacklistService.AddAsync(request.info_hash, request.reason, context.RequestAborted);

            return result switch
            {
                BlacklistResult.InvalidHash => Results.BadRequest("info_hash is required"),
                BlacklistResult.InvalidReason => Results.BadRequest("reason is required"),
                BlacklistResult.AlreadyBlacklisted => Results.Conflict("Item already blacklisted"),
                BlacklistResult.Added => Results.NoContent(),
                _ => Results.BadRequest("An error occurred while adding a blacklisted item")
            };
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occurred while adding a blacklisted item");
            return Results.BadRequest("An error occurred while adding a blacklisted item");
        }
    }

    private abstract class BlacklistLogger;
}
