using Microsoft.EntityFrameworkCore.Design;

namespace Zilean.Database;

/// <summary>
/// Design-time factory for <see cref="ZileanDbContext"/>, used by <c>dotnet ef migrations</c>
/// to instantiate the context without running the application's DI bootstrap.
/// </summary>
public class ZileanDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ZileanDbContext>
{
    /// <summary>
    /// Creates a <see cref="ZileanDbContext"/> using the connection string from the
    /// <c>Zilean__Database__ConnectionString</c> environment variable, falling back to a
    /// localhost default.
    /// </summary>
    /// <param name="args">Arguments from the EF Core command line (unused).</param>
    /// <returns>A configured <see cref="ZileanDbContext"/> instance.</returns>
    public ZileanDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("Zilean__Database__ConnectionString")
            ?? "Host=localhost;Port=5432;Database=zilean;Username=postgres;Password=postgres;Include Error Detail=true;";

        var optionsBuilder = new DbContextOptionsBuilder<ZileanDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ZileanDbContext(optionsBuilder.Options);
    }
}