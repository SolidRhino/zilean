namespace Zilean.Shared.Features.Configuration;

/// <summary>
/// Extension methods for registering Zilean configuration in the DI service container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="ZileanConfiguration"/> instance as a singleton in the service collection.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configuration">The configuration instance to register.</param>
    /// <returns>The same <paramref name="services"/> collection for chaining.</returns>
    public static IServiceCollection AddConfiguration(this IServiceCollection services, ZileanConfiguration configuration)
    {
        services.AddSingleton(configuration);

        return services;
    }
}