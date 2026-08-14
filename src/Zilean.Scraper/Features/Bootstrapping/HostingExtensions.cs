namespace Zilean.Scraper.Features.Bootstrapping;

/// <summary>
/// Extension methods for wiring Spectre.Console CLI command application into the DI container and host lifecycle.
/// </summary>
public static class HostingExtensions
{
    /// <summary>
    /// Registers a <see cref="ICommandApp"/> with a default command and configuration callback.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configurator">Callback to configure the command application.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCommandLine(
        this IServiceCollection services,
        Action<IConfigurator> configurator)
    {
        var app = new CommandApp(new TypeRegistrar(services));
        app.Configure(configurator);
        services.AddSingleton<ICommandApp>(app);

        return services;
    }

    /// <summary>
    /// Registers a <see cref="ICommandApp"/> with a specified default command type and configuration callback.
    /// </summary>
    /// <typeparam name="TDefaultCommand">The default command type.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configurator">Callback to configure the command application.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCommandLine<TDefaultCommand>(
        this IServiceCollection services,
        Action<IConfigurator> configurator)
        where TDefaultCommand : class, ICommand
    {
        var app = new CommandApp<TDefaultCommand>(new TypeRegistrar(services));
        app.Configure(configurator);
        services.AddSingleton<ICommandApp>(app);

        return services;
    }

    /// <summary>
    /// Starts the host, runs the configured CLI command application with the given arguments, then stops and disposes the host.
    /// </summary>
    /// <param name="host">The application host.</param>
    /// <param name="args">Command-line arguments to pass to the CLI.</param>
    /// <returns>The exit code from the CLI command application.</returns>
    public static async Task<int> RunAsync(this IHost host, string[] args)
    {
        ArgumentNullException.ThrowIfNull(host);

        await host.StartAsync();

        try
        {
            var app = host.Services.GetService<ICommandApp>() ??
                      throw new InvalidOperationException("Command application has not been configured.");

            return await app.RunAsync(args);
        }
        finally
        {
            await host.StopAsync();
            await ((IAsyncDisposable)host).DisposeAsync();
        }
    }
}
