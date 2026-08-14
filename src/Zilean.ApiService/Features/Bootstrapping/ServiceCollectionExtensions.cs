using Microsoft.AspNetCore.Components.Authorization;
using Zilean.ApiService.Features.Dashboard.Components.Pages.Dashboard;

namespace Zilean.ApiService.Features.Bootstrapping;

/// <summary>
/// Extension methods for registering Zilean ApiService services and middleware into the DI container.
/// </summary>
[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers OpenAPI document generation with the <see cref="ApiKeyDocumentTransformer"/> for API key security metadata.
    /// </summary>
    /// <param name="services">The service collection to add OpenAPI support to.</param>
    /// <returns>The <paramref name="services"/> collection for chaining.</returns>
    public static IServiceCollection AddSwaggerSupport(this IServiceCollection services) =>
        services.AddOpenApi("v2", options =>
        {
            options.AddDocumentTransformer<ApiKeyDocumentTransformer>();
        });

    /// <summary>
    /// Registers Coravel scheduler support.
    /// </summary>
    /// <param name="services">The service collection to add scheduling support to.</param>
    /// <returns>The <paramref name="services"/> collection for chaining.</returns>
    public static IServiceCollection AddSchedulingSupport(this IServiceCollection services) =>
        services.AddScheduler();

    /// <summary>
    /// Registers the <see cref="StartupService"/> and <see cref="ConfigurationUpdaterService"/> hosted services.
    /// </summary>
    /// <param name="services">The service collection to add the hosted services to.</param>
    /// <returns>The <paramref name="services"/> collection for chaining.</returns>
    public static IServiceCollection AddStartupHostedServices(this IServiceCollection services) =>
        services.AddHostedService<StartupService>()
            .AddHostedService<ConfigurationUpdaterService>();

    /// <summary>
    /// Registers the sync job types (<see cref="DmmSyncJob"/>, <see cref="GenericSyncJob"/>) and the <see cref="SyncOnDemandState"/> singleton.
    /// </summary>
    /// <param name="services">The service collection to register the sync jobs into.</param>
    /// <param name="configuration">The Zilean configuration (unused for registration but reserved for future gating).</param>
    /// <returns>The <paramref name="services"/> collection for chaining.</returns>
    public static IServiceCollection RegisterSyncJobs(this IServiceCollection services, ZileanConfiguration configuration)
    {
        services.AddTransient<DmmSyncJob>();
        services.AddTransient<GenericSyncJob>();
        services.AddSingleton<SyncOnDemandState>();

        return services;
    }

    /// <summary>
    /// Configures the Coravel scheduler to run <see cref="DmmSyncJob"/> and <see cref="GenericSyncJob"/> on their configured cron schedules when scraping is enabled.
    /// </summary>
    /// <param name="provider">The application service provider used to resolve the scheduler.</param>
    /// <param name="configuration">The Zilean configuration containing DMM and ingestion scrape schedules.</param>
    /// <returns>The <paramref name="provider"/> for chaining.</returns>
    public static IServiceProvider SetupScheduling(this IServiceProvider provider, ZileanConfiguration configuration)
    {
        provider.UseScheduler(scheduler =>
            {
                if (configuration.Dmm.EnableScraping)
                {
                    scheduler.Schedule<DmmSyncJob>()
                        .Cron(configuration.Dmm.ScrapeSchedule)
                        .PreventOverlapping("SyncJobs");
                }

                if (configuration.Ingestion.EnableScraping)
                {
                    scheduler.Schedule<GenericSyncJob>()
                        .Cron(configuration.Ingestion.ScrapeSchedule)
                        .PreventOverlapping("SyncJobs");
                }
            })
            .LogScheduledTaskProgress();

        return provider;
    }

    /// <summary>
    /// Registers API key authentication and dashboard cookie authentication schemes, plus the associated authorization policies.
    /// </summary>
    /// <param name="services">The service collection to add authentication services to.</param>
    /// <returns>The <paramref name="services"/> collection for chaining.</returns>
    public static IServiceCollection AddApiKeyAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = "None";
                options.DefaultAuthenticateScheme = "None";
            })
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthentication.Scheme, _ => { })
            .AddCookie(ApiKeyAuthentication.DashboardScheme, options =>
            {
                options.Cookie.Name = "ZileanDashboard";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.SlidingExpiration = true;
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
                options.AccessDeniedPath = "/login";
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(ApiKeyAuthentication.Policy, policy => policy.RequireAuthenticatedUser().AddAuthenticationSchemes(ApiKeyAuthentication.Scheme));
            options.AddPolicy(ApiKeyAuthentication.DashboardPolicy, policy => policy.RequireAuthenticatedUser().AddAuthenticationSchemes(ApiKeyAuthentication.DashboardScheme));
        });

        return services;
    }

    /// <summary>
    /// Registers Blazor dashboard services (Razor components, Syncfusion, cascading auth state, and dashboard data services) when the dashboard is enabled.
    /// </summary>
    /// <param name="services">The service collection to add dashboard services to.</param>
    /// <param name="configuration">The Zilean configuration; if <c>EnableDashboard</c> is false, no services are registered.</param>
    /// <returns>The <paramref name="services"/> collection for chaining.</returns>
    public static IServiceCollection AddDashboardSupport(this IServiceCollection services, ZileanConfiguration configuration)
    {
        if (!configuration.EnableDashboard)
        {
            return services;
        }

        services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        services.AddCascadingAuthenticationState();

        services.AddSyncfusionBlazor();

        services.AddScoped<DashboardDataAdapter>();
        services.AddSingleton<PythonRuntimeService>();
        services.AddSingleton<TorrentParser>();

        return services;
    }
}