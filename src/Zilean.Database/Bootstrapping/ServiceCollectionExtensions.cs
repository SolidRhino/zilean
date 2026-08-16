namespace Zilean.Database.Bootstrapping;

/// <summary>
/// Extension methods for registering Zilean data-access services into the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="ZileanDbContext"/> factory and all Zilean data services
    /// (torrent info, IMDb files, IMDb matching, blacklist, torrent queries) into <paramref name="services"/>.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configuration">The Zilean configuration providing the database connection string and IMDb matching strategy.</param>
    /// <returns>The <paramref name="services"/> collection, for chaining.</returns>
    public static IServiceCollection AddZileanDataServices(this IServiceCollection services, ZileanConfiguration configuration)
    {
        services.AddDbContextFactory<ZileanDbContext>(options => options.UseNpgsql(configuration.Database.ConnectionString));
        services.AddTransient<ITorrentInfoService, TorrentInfoService>();
        services.AddTransient<IImdbFileService, ImdbFileService>();
        services.RegisterImdbMatchingService(configuration);
        services.AddTransient<IBlacklistService, BlacklistService>();
        services.AddTransient<ITorrentsQueryService, TorrentsQueryService>();
        services.AddTransient<DmmService>();

        return services;
    }

    private static void RegisterImdbMatchingService(this IServiceCollection services, ZileanConfiguration configuration)
    {
        // Singleton so the in-memory IMDb data (Lucene index or partitioned dictionaries)
        // is populated once per process and reused across every batch in an ingestion run,
        // instead of being rebuilt per 5K-torrent batch in TorrentInfoService.StoreTorrentInfo.
        if (configuration.Imdb.UseLucene)
        {
            services.AddSingleton<IImdbMatchingService, ImdbLuceneMatchingService>();
            return;
        }

        services.AddSingleton<IImdbMatchingService, ImdbFuzzyStringMatchingService>();
    }
}