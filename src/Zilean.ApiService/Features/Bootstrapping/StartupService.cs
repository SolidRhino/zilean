namespace Zilean.ApiService.Features.Bootstrapping;

/// <summary>
/// Hosted lifecycle service that validates configuration, waits for the database, applies EF Core migrations, and triggers the first DMM sync on startup.
/// </summary>
/// <param name="configuration">The Zilean configuration to validate and inspect.</param>
/// <param name="serviceProvider">The application service provider used to resolve scoped services like <see cref="ZileanDbContext"/>.</param>
/// <param name="loggerFactory">The logger factory used to create loggers for startup diagnostics.</param>
public class StartupService(
    ZileanConfiguration configuration,
    IServiceProvider serviceProvider,
    ILoggerFactory loggerFactory) : IHostedLifecycleService
{
    private const int MaxRetries = 5;
    private static readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// No-op; called by the host before <see cref="StartingAsync"/>. Startup work is performed in <see cref="StartingAsync"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A completed task.</returns>
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// No-op; called by the host after <see cref="StoppingAsync"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Runs early in the host startup pipeline: warns about insecure database passwords, validates configuration, waits for the database to be reachable, and applies pending migrations.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that completes when the database is ready and migrations have been applied.</returns>
    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger<StartupService>();

        // Security check — warn about insecure Postgres credentials
        if (configuration.Database.HasInsecurePassword())
        {
            logger.LogWarning("SECURITY WARNING: PostgreSQL password is empty or set to the default 'postgres'. " +
                "This is a security risk — if your database port is exposed, attackers can connect and compromise your system. " +
                "Set a strong password via POSTGRES_PASSWORD or Zilean__Database__ConnectionString.");
        }

        // Validate configuration before proceeding
        var validationErrors = configuration.Validate();
        if (validationErrors.Count > 0)
        {
            foreach (var error in validationErrors)
            {
                logger.LogError("Configuration error: {Error}", error);
            }
            throw new InvalidOperationException($"Zilean configuration is invalid: {string.Join("; ", validationErrors)}");
        }

        // Wait for database with retry
        await WaitForDatabaseAsync(logger, cancellationToken);

        logger.LogInformation("Applying Migrations...");
        await using var asyncScope = serviceProvider.CreateAsyncScope();
        var dbContext = asyncScope.ServiceProvider.GetRequiredService<ZileanDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Migrations Applied.");
    }

    private async Task WaitForDatabaseAsync(ILogger logger, CancellationToken cancellationToken)
    {
        var retries = 0;
        while (retries < MaxRetries)
        {
            try
            {
                await using var connection = new Npgsql.NpgsqlConnection(configuration.Database.ConnectionString);
                await connection.OpenAsync(cancellationToken);
                logger.LogInformation("Database connection established on attempt {Attempt}.", retries + 1);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                retries++;
                var host = GetConnectionHost(configuration.Database.ConnectionString);
                var database = GetConnectionDatabase(configuration.Database.ConnectionString);
                logger.LogWarning("Database not ready (attempt {Attempt}/{MaxRetries}). Host: {Host}, Database: {Database}. Retrying in {Delay}s. Error: {Error}",
                    retries, MaxRetries, host, database, _retryDelay.TotalSeconds, ex.Message);
                await Task.Delay(_retryDelay, cancellationToken);
            }
        }

        throw new InvalidOperationException($"Could not connect to the database after {MaxRetries} attempts. Please check your connection string and ensure the database is running.");
    }

    private static string GetConnectionHost(string connectionString)
    {
        try { return new Npgsql.NpgsqlConnectionStringBuilder(connectionString).Host ?? "unknown"; }
        catch { return "unknown"; }
    }

    private static string GetConnectionDatabase(string connectionString)
    {
        try { return new Npgsql.NpgsqlConnectionStringBuilder(connectionString).Database ?? "unknown"; }
        catch { return "unknown"; }
    }

    /// <summary>
    /// No-op; called by the host after <see cref="StopAsync"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A completed task.</returns>
    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// No-op; called by the host before <see cref="StopAsync"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A completed task.</returns>
    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Runs after the host has fully started: triggers the first DMM sync job if scraping is enabled and no pages have been parsed yet.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that completes when the first-run sync check is done.</returns>
    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger<StartupService>();

        if (configuration.Dmm.EnableScraping)
        {
            await using var asyncScope = serviceProvider.CreateAsyncScope();
            var dmmJob = asyncScope.ServiceProvider.GetRequiredService<DmmSyncJob>();
            var pagesExist = await dmmJob.ShouldRunOnStartup();
            if (!pagesExist)
            {
                await dmmJob.Invoke();
            }
        }

        logger.LogInformation("Zilean Running: Startup Complete.");
    }
}