using System.Net;
using System.Text.Json;

namespace Zilean.Tests.Tests;

/// <summary>
/// Integration test: /healthchecks/ready returns 200 "degraded" with
/// pythonAvailable=false when ZILEAN_PYTHON_PYLIB is unset. Uses a dashboard-enabled
/// factory (the Python check is gated on EnableDashboard). The env var is overridden
/// AFTER CreateClient (so the host starts cleanly) but BEFORE the /ready request (so
/// the lazy PythonRuntimeService singleton faults on first resolution).
/// </summary>
[Collection(nameof(ApiTestCollection))]
public class PythonUnavailableHealthCheckTests : IAsyncLifetime
{
    private readonly PostgresLifecycleFixture _fixture;
    private ZileanWebApplicationFactory _dashboardFactory = null!;
    private HttpClient _client = null!;
    private string? _savedPython;

    public PythonUnavailableHealthCheckTests(PostgresLifecycleFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _dashboardFactory = new ZileanWebApplicationFactory(
            _fixture.ZileanConfiguration.Database.ConnectionString!,
            enableDashboard: true);

        // CreateClient blocks until the host is fully started (migrations run, DB connected).
        _client = _dashboardFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
        });

        // Capture the env var BEFORE overriding it so DisposeAsync can restore it.
        _savedPython = Environment.GetEnvironmentVariable("ZILEAN_PYTHON_PYLIB");

        await Task.CompletedTask;
    }

    [Fact]
    public async Task Ready_PythonUnavailable_Returns200Degraded()
    {
        // Override AFTER CreateClient so the host started cleanly; BEFORE the /ready
        // request so the lazy PythonRuntimeService singleton faults on first resolution.
        Environment.SetEnvironmentVariable("ZILEAN_PYTHON_PYLIB", "");

        var response = await _client.GetAsync("/healthchecks/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "because the database is healthy; only Python is unavailable → degraded, not unhealthy");

        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.GetProperty("status").GetString().Should().Be("degraded",
            "because pythonAvailable is false while database is true");
        root.GetProperty("database").GetBoolean().Should().BeTrue(
            "because the shared Postgres fixture is healthy");
        root.GetProperty("pythonAvailable").GetBoolean().Should().BeFalse(
            "because ZILEAN_PYTHON_PYLIB was emptied before the /ready request");
    }

    public Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ZILEAN_PYTHON_PYLIB", _savedPython);
        _dashboardFactory.Dispose();
        return Task.CompletedTask;
    }
}