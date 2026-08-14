using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Zilean.Shared.Features.Python;

namespace Zilean.Tests.Fixtures;

/// <summary>
/// In-process <see cref="WebApplicationFactory{Program}"/> that hosts the Zilean API
/// with a testing configuration: disables scraping/ingestion, enables dmm/torznab/torrents/imdb
/// endpoints, injects the test Postgres connection string, sets <c>ZILEAN_PYTHON_PYLIB</c> to
/// empty so <see cref="PythonRuntimeService"/> faults cleanly (no native DLL load attempt),
/// and removes <see cref="Zilean.ApiService.Features.Bootstrapping.ConfigurationUpdaterService"/>
/// so it does not write config files to disk during tests. Migrations still run via
/// <see cref="Zilean.ApiService.Features.Bootstrapping.StartupService"/>.
/// </summary>
public class ZileanWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly bool _enableDashboard;
    private readonly Dictionary<string, string?> _envVars = [];

    /// <summary>
    /// Creates a factory with the given connection string and dashboard disabled.
    /// </summary>
    /// <param name="connectionString">The Postgres connection string to inject.</param>
    public ZileanWebApplicationFactory(string connectionString) : this(connectionString, enableDashboard: false)
    {
    }

    /// <summary>
    /// Creates a factory with the given connection string and optional dashboard support.
    /// </summary>
    /// <param name="connectionString">The Postgres connection string to inject.</param>
    /// <param name="enableDashboard">Whether to enable the Blazor dashboard.</param>
    public ZileanWebApplicationFactory(string connectionString, bool enableDashboard)
    {
        _connectionString = connectionString;
        _enableDashboard = enableDashboard;
    }

    /// <summary>
    /// Configures the test host: sets env vars, switches to the "Testing" environment,
    /// injects in-memory config overrides, and removes <see cref="Zilean.ApiService.Features.Bootstrapping.ConfigurationUpdaterService"/>.
    /// </summary>
    /// <param name="builder">The web host builder.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set ZILEAN_PYTHON_PYLIB to empty so PythonRuntimeService.InitializePythonEngine
        // returns a faulted Task (InvalidOperationException) without attempting a native
        // DLL load that would log a scary DllNotFoundException. The runtime's IsAvailable
        // property returns false, and all API endpoints gracefully degrade without Python.
        SetEnvVar("ZILEAN_PYTHON_PYLIB", "");
        SetEnvVar("ZILEAN_PYTHON_VENV", "");

        // DatabaseConfiguration constructor reads this env var directly, bypassing config binding.
        SetEnvVar("Zilean__Database__ConnectionString", _connectionString);
        // AddConfigurationFiles() adds env vars last, which override the in-memory collection
        // below. Set EnableDashboard via env var so it wins the binding for the dashboard-enabled factory.
        SetEnvVar("Zilean__EnableDashboard", _enableDashboard ? "true" : "false");
        // Env vars override settings.json (added last by AddConfigurationFiles), so register the
        // protected /torrents routes (checkcached, all) for auth-middleware tests. settings.json
        // defaults these to false, so the in-memory collection alone is insufficient.
        SetEnvVar("Zilean__Torrents__EnableEndpoint", "true");
        SetEnvVar("Zilean__Torrents__EnableCacheCheckEndpoint", "true");
        SetEnvVar("Zilean__Torrents__EnableScrapeEndpoint", "true");

        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Zilean:Database:ConnectionString"] = _connectionString,
                ["Zilean:Dmm:EnableScraping"] = "false",
                ["Zilean:EnableDashboard"] = _enableDashboard.ToString().ToLowerInvariant(),
                ["Zilean:Dmm:EnableEndpoint"] = "true",
                ["Zilean:Torznab:EnableEndpoint"] = "true",
                ["Zilean:Torrents:EnableEndpoint"] = "true",
                ["Zilean:Imdb:EnableEndpoint"] = "true",
                ["Zilean:Imdb:EnableImportMatching"] = "false",
                ["Zilean:Ingestion:EnableScraping"] = "false",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove only ConfigurationUpdaterService (writes config files to disk).
            // Leave StartupService intact - it runs migrations and waits for DB.
            var descriptor = services.FirstOrDefault(
                d => d.ImplementationType == typeof(Zilean.ApiService.Features.Bootstrapping.ConfigurationUpdaterService));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }
        });
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> with the <c>X-API-KEY</c> header pre-attached,
    /// resolving the running app's configured API key from DI. Mirrors the key-resolution
    /// pattern used by DashboardAuthTests so tests can hit protected endpoints.
    /// </summary>
    /// <returns>An <see cref="HttpClient"/> authenticated with the app's API key.</returns>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        using var scope = Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<ZileanConfiguration>();
        client.DefaultRequestHeaders.Add("X-API-KEY", config.ApiKey);
        return client;
    }

    private void SetEnvVar(string key, string? value)
    {
        if (!_envVars.ContainsKey(key))
        {
            _envVars[key] = Environment.GetEnvironmentVariable(key);
        }
        Environment.SetEnvironmentVariable(key, value);
    }

    /// <summary>
    /// Restores env vars to their pre-test values, then disposes the factory.
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources.</param>
    protected override void Dispose(bool disposing)
    {
        foreach (var (key, originalValue) in _envVars)
        {
            Environment.SetEnvironmentVariable(key, originalValue);
        }

        _envVars.Clear();
        base.Dispose(disposing);
    }
}