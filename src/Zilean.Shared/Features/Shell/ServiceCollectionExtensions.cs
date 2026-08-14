namespace Zilean.Shared.Features.Shell;

/// <summary>
/// Extension methods for registering shell execution services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ShellExecutionService"/> as a singleton <see cref="IShellExecutionService"/>.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddShellExecutionService(this IServiceCollection services)
    {
        services.AddSingleton<IShellExecutionService, ShellExecutionService>();
        return services;
    }
}