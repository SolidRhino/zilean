using Microsoft.AspNetCore.Components.Authorization;
using Zilean.ApiService.Features.Dashboard.Components.Pages.Dashboard;

namespace Zilean.ApiService.Features.Bootstrapping;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSwaggerSupport(this IServiceCollection services) =>
        services.AddOpenApi("v2", options =>
        {
            options.AddDocumentTransformer<ApiKeyDocumentTransformer>();
        });

    public static IServiceCollection AddSchedulingSupport(this IServiceCollection services) =>
        services.AddScheduler();

    public static IServiceCollection AddStartupHostedServices(this IServiceCollection services) =>
        services.AddHostedService<StartupService>()
            .AddHostedService<ConfigurationUpdaterService>();

    public static IServiceCollection RegisterSyncJobs(this IServiceCollection services, ZileanConfiguration configuration)
    {
        services.AddTransient<DmmSyncJob>();
        services.AddTransient<GenericSyncJob>();
        services.AddSingleton<SyncOnDemandState>();

        return services;
    }

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
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromHours(12);
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
                options.AccessDeniedPath = "/login";
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(ApiKeyAuthentication.Policy, policy =>
            {
                policy.AuthenticationSchemes.Add(ApiKeyAuthentication.Scheme);
                policy.RequireAuthenticatedUser();
            });
            options.AddPolicy(ApiKeyAuthentication.DashboardPolicy, policy =>
            {
                policy.AuthenticationSchemes.Add(ApiKeyAuthentication.DashboardScheme);
                policy.RequireAuthenticatedUser();
            });
        });

        return services;
    }

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
        services.AddSingleton<ParseTorrentNameService>();

        return services;
    }
}
