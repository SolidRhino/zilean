using Npgsql;

namespace Zilean.Shared.Features.Configuration;

/// <summary>
/// PostgreSQL database connection configuration. The connection string is built from
/// individual <c>POSTGRES_*</c> env vars or overridden directly via
/// <c>Zilean__Database__ConnectionString</c>.
/// </summary>
public class DatabaseConfiguration
{
    /// <summary>
    /// The PostgreSQL connection string used by EF Core. Not serialized to JSON.
    /// Built from <c>POSTGRES_*</c> env vars or set via <c>Zilean__Database__ConnectionString</c>.
    /// </summary>
    [JsonIgnore]
    public string ConnectionString { get; set; }

    /// <summary>
    /// Initializes a new instance, building the connection string from
    /// <c>Zilean__Database__ConnectionString</c> or individual <c>POSTGRES_*</c> env vars.
    /// </summary>
    public DatabaseConfiguration()
    {
        // Check for full connection string first (backwards compat with v3.5.0)
        var fullConnString = Environment.GetEnvironmentVariable("Zilean__Database__ConnectionString");
        if (!string.IsNullOrWhiteSpace(fullConnString))
        {
            ConnectionString = fullConnString;
            return;
        }

        // Build from individual env vars
        var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
        var db = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "zilean";
        var user = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
        var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "";

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = int.Parse(port),
            Database = db,
            Username = user,
            Password = password,
            IncludeErrorDetail = true,
            Timeout = 30,
            CommandTimeout = 3600,
        };

        ConnectionString = builder.ConnectionString;
    }

    /// <summary>
    /// Returns true if the configured password is empty or a known insecure default.
    /// </summary>
    /// <returns><c>true</c> if the password is empty or <c>postgres</c>; otherwise <c>false</c>.</returns>
    public bool HasInsecurePassword()
    {
        try
        {
            var parsed = new NpgsqlConnectionStringBuilder(ConnectionString);
            return string.IsNullOrEmpty(parsed.Password) ||
                   string.Equals(parsed.Password, "postgres", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}