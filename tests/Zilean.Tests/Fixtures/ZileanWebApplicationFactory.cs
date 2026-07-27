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
        // Set dummy Python env vars to prevent Environment.Exit in ParseTorrentNameService
        Environment.SetEnvironmentVariable("ZILEAN_PYTHON_PYLIB", "/dummy/libpython3.so");
        Environment.SetEnvironmentVariable("ZILEAN_PYTHON_VENV", "/dummy/venv");

        // DatabaseConfiguration constructor reads this env var directly, bypassing config binding.
        Environment.SetEnvironmentVariable("Zilean__Database__ConnectionString", _connectionString);
        // AddConfigurationFiles() adds env vars last, which override the in-memory collection
        // below. Set EnableDashboard via env var so it wins the binding for the dashboard-enabled factory.
        Environment.SetEnvironmentVariable("Zilean__EnableDashboard", _enableDashboard ? "true" : "false");

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
}
