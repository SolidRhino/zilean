namespace Zilean.ApiService.Features.Bootstrapping;

/// <summary>
/// Extension methods for configuring the ASP.NET Core middleware pipeline and mapping all Zilean endpoints.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Configures required middleware: dashboard static files, exception handling, antiforgery (when the dashboard is enabled), followed by authentication and authorization.
    /// </summary>
    /// <param name="app">The web application to configure middleware for.</param>
    /// <param name="configuration">The Zilean configuration controlling dashboard-specific middleware.</param>
    /// <returns>The <paramref name="app"/> for chaining.</returns>
    public static WebApplication UseZileanRequired(this WebApplication app, ZileanConfiguration configuration)
    {
        if (configuration.EnableDashboard)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                app.UseHsts();
            }

            app.UseStaticFiles();
            app.UseAntiforgery();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    /// <summary>
    /// Maps all Zilean API endpoints (DMM, IMDB, Torznab, torrents, blacklist, health checks), the dashboard Razor components (when enabled), and the OpenAPI/Scalar UI.
    /// </summary>
    /// <param name="app">The web application to map endpoints onto.</param>
    /// <param name="configuration">The Zilean configuration controlling which endpoint groups and the dashboard are enabled.</param>
    /// <returns>The <paramref name="app"/> for chaining.</returns>
    public static WebApplication MapZileanEndpoints(this WebApplication app, ZileanConfiguration configuration)
    {
        app.MapDefaultEndpoints();

        app.MapDmmEndpoints(configuration)
            .MapImdbEndpoints(configuration)
            .MapTorznabEndpoints(configuration)
            .MapTorrentsEndpoints(configuration)
            .MapBlacklistEndpoints()
            .MapHealthCheckEndpoints();

        if (configuration.EnableDashboard)
        {
            app.MapStaticAssets();

            app.MapDashboardAuthEndpoints();

            app.MapRazorComponents<Dashboard.Components.ZileanWebApp>()
                .AddInteractiveServerRenderMode()
                .AddInteractiveWebAssemblyRenderMode()
                .RequireAuthorization(ApiKeyAuthentication.DashboardPolicy);

            if (!string.IsNullOrWhiteSpace(configuration.SyncfusionLicense))
            {
                Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(configuration.SyncfusionLicense);
            }
        }

        app.MapOpenApi();
        app.MapScalarApiReference();

        return app;
    }
}