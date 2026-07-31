namespace Zilean.Database.Bootstrapping;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddZileanDataServices(this IServiceCollection services, ZileanConfiguration configuration)
    {
        services.AddDbContextFactory<ZileanDbContext>(options => options.UseNpgsql(configuration.Database.ConnectionString));
        services.AddTransient<ITorrentInfoService, TorrentInfoService>();
        services.AddTransient<IImdbFileService, ImdbFileService>();
        services.RegisterImdbMatchingService(configuration);
        services.AddTransient<IBlacklistService, BlacklistService>();
        services.AddTransient<ITorrentsQueryService, TorrentsQueryService>();

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
