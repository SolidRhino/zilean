namespace Zilean.Tests.Fixtures;

/// <summary>
/// Shared test fixture that starts an ephemeral Postgres container, builds the in-process
/// API host (which applies EF Core migrations on startup), seeds
/// canonical test data via <see cref="TestDataBuilder.SeedAsync"/>, and runs
/// <c>ANALYZE "Torrents"</c> so pg_trgm similarity returns correct results on the small seed set.
/// Shared across all <see cref="ApiTestCollection"/> tests via xUnit collection fixtures.
/// </summary>
public class PostgresLifecycleFixture : IAsyncLifetime
{
    /// <summary>
    /// The ephemeral PostgreSQL 16.3 container instance.
    /// </summary>
    private PostgreSqlContainer PostgresContainer { get; } = new PostgreSqlBuilder()
        .WithImage("postgres:16.3-alpine3.20")
        .WithEnvironment("POSTGRES_USER", "postgres")
        .WithEnvironment("POSTGRES_PASSWORD", "postgres")
        .WithEnvironment("POSTGRES_DB", "zilean")
        .Build();

    /// <summary>
    /// The Zilean configuration populated with the container's connection string.
    /// </summary>
    public ZileanConfiguration ZileanConfiguration { get; } = new();

    /// <summary>
    /// The in-process <see cref="ZileanWebApplicationFactory"/> hosting the API.
    /// </summary>
    public ZileanWebApplicationFactory Factory { get; private set; } = null!;

    /// <summary>
    /// Resolves the <see cref="IDbContextFactory{ZileanDbContext}"/> from the running host's DI container.
    /// </summary>
    public IDbContextFactory<ZileanDbContext> DbContextFactory => Factory.Services.GetRequiredService<IDbContextFactory<ZileanDbContext>>();

    /// <summary>
    /// Configures Verify.Xunit snapshot paths to the <c>Verification/</c> directory.
    /// </summary>
    public PostgresLifecycleFixture() =>
        DerivePathInfo(
            (_, projectDirectory, type, method) => new(
                directory: Path.Combine(projectDirectory, "Verification"),
                typeName: type.Name,
                methodName: method.Name));

    /// <summary>
    /// Starts the Postgres container, creates the web factory, forces host startup (which applies
    /// migrations), seeds test data, and updates table statistics.
    /// </summary>
    /// <returns>A task that completes when the fixture is ready for tests.</returns>
    public async Task InitializeAsync()
    {
        await PostgresContainer.StartAsync();
        var connectionString = PostgresContainer.GetConnectionString();
        ZileanConfiguration.Database.ConnectionString = connectionString;
        Factory = new ZileanWebApplicationFactory(connectionString);

        // Force host startup (runs migrations via StartupService)
        // CreateClient() blocks until the host is fully started
        using var client = Factory.CreateClient();

        // Seed test data once, after migrations are applied
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ZileanDbContext>();
        await TestDataBuilder.SeedAsync(dbContext);

        // Update pg_trgm statistics so trigram similarity search works on seeded data
        await dbContext.Database.ExecuteSqlRawAsync("ANALYZE \"Torrents\";");
    }

    /// <summary>
    /// Disposes the web factory and the Postgres container.
    /// </summary>
    /// <returns>A task that completes when teardown is done.</returns>
    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await PostgresContainer.DisposeAsync();
    }
}