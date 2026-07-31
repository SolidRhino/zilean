using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Zilean.Shared.Features.Python;

namespace Zilean.Tests.Fixtures;

public class ZileanWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly bool _enableDashboard;
    private readonly Dictionary<string, string?> _envVars = [];

    public ZileanWebApplicationFactory(string connectionString) : this(connectionString, enableDashboard: false)
    {
    }

    public ZileanWebApplicationFactory(string connectionString, bool enableDashboard)
    {
        _connectionString = connectionString;
        _enableDashboard = enableDashboard;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set dummy Python env vars to prevent Environment.Exit in PythonRuntimeService
        SetEnvVar("ZILEAN_PYTHON_PYLIB", "/dummy/libpython3.so");
        SetEnvVar("ZILEAN_PYTHON_VENV", "/dummy/venv");

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