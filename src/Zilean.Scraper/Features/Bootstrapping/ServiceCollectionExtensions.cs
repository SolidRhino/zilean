using Zilean.Scraper.Features.Ingestion.Dmm;

namespace Zilean.Scraper.Features.Bootstrapping;

/// <summary>
/// Extension methods for registering scraper and ingestion services into the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all scraper services: HTTP client, configuration, DMM, generic ingestion, IMDb, database, Python runtime, and migration hosted service.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration.</param>
    public static void AddScrapers(this IServiceCollection services, IConfiguration configuration)
    {
        var zileanConfiguration = configuration.GetZileanConfiguration();

        services.AddHttpClient();
        services.AddSingleton(zileanConfiguration);
        services.AddImdbServices();
        services.AddDmmServices();
        services.AddGenericServices();
        services.AddZileanDataServices(zileanConfiguration);
        services.AddSingleton<PythonRuntimeService>();
        services.AddSingleton<TorrentParser>();
        services.AddHostedService<EnsureMigrated>();
    }

    private static void AddDmmServices(this IServiceCollection services)
    {
        services.AddSingleton<DmmFileDownloader>();
        services.AddSingleton<DmmScraping>();
        services.AddTransient<DmmService>();
    }

    private static void AddGenericServices(this IServiceCollection services)
    {
        services.AddSingleton<GenericIngestionScraping>();
        services.AddSingleton<KubernetesServiceDiscovery>();
    }

    private static void AddImdbServices(this IServiceCollection services)
    {
        services.AddSingleton<ImdbMetadataLoader>();
        services.AddSingleton<ImdbConfiguration>();
        services.AddSingleton<ImdbFileDownloader>();
        services.AddSingleton<ImdbFileProcessor>();
        services.AddSingleton<ImdbFileService>();
    }
}
