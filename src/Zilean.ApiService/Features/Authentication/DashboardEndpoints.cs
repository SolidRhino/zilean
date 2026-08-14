namespace Zilean.ApiService.Features.Authentication;

/// <summary>
/// Maps the dashboard authentication endpoints: <c>/auth/login</c> (validates the API key and issues a dashboard cookie) and <c>/logout</c> (clears the cookie).
/// </summary>
public static class DashboardEndpoints
{
    /// <summary>
    /// Maps the <c>/auth/login</c> and <c>/logout</c> endpoints for dashboard cookie-based authentication.
    /// </summary>
    /// <param name="app">The endpoint route builder to map the endpoints onto.</param>
    /// <returns>The <paramref name="app"/> so additional endpoint mapping calls can be chained.</returns>
    public static IEndpointRouteBuilder MapDashboardAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (HttpContext context, ZileanConfiguration configuration) =>
        {
            var submittedKey = context.Request.Headers.TryGetValue("X-API-KEY", out var headerKey)
                ? headerKey.ToString()
                : context.Request.HasFormContentType
                    ? (await context.Request.ReadFormAsync())["apiKey"].ToString()
                    : string.Empty;

            var configuredKey = configuration.ApiKey;
            if (string.IsNullOrEmpty(configuredKey) ||
                string.IsNullOrEmpty(submittedKey) ||
                !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(submittedKey),
                    Encoding.UTF8.GetBytes(configuredKey)))
            {
                return Results.Unauthorized();
            }

            var claims = new[] { new Claim(ClaimTypes.Name, "DashboardUser") };
            var identity = new ClaimsIdentity(claims, ApiKeyAuthentication.DashboardScheme);
            var principal = new ClaimsPrincipal(identity);

            await context.SignInAsync(ApiKeyAuthentication.DashboardScheme, principal);
            return Results.LocalRedirect("/dashboard");
        });

        app.MapPost("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(ApiKeyAuthentication.DashboardScheme);
            return Results.LocalRedirect("/login");
        }).RequireAuthorization(ApiKeyAuthentication.DashboardPolicy);

        return app;
    }
}