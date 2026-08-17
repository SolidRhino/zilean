using System.Net;

namespace Zilean.Tests.Tests;

/// <summary>
/// Integration tests for the on-demand generic-sync endpoint (<c>/dmm/on-demand-generic-sync</c>).
/// The endpoint is gated by <c>Ingestion.EnableScraping</c>, which is false in the test fixture.
/// These tests verify the endpoint is not mapped (404) when scraping is disabled, proving the
/// config gate works without attempting to shell-execute the scraper binary.
/// </summary>
[Collection(nameof(ApiTestCollection))]
public class GenericSyncEndpointTests(PostgresLifecycleFixture fixture)
{
    [Fact]
    public async Task OnDemandGenericSync_WithScrapingDisabled_Returns404()
    {
        var client = fixture.Factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/dmm/on-demand-generic-sync");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because Ingestion.EnableScraping is false in the test config, so the endpoint is not mapped");
    }
}

/// <summary>
/// Integration test for the on-demand generic-sync endpoint when scraping is enabled.
/// Sets the <c>Zilean__Ingestion__EnableScraping</c> env var before host startup so the
/// endpoint is mapped. Verifies that an unauthenticated request returns 401 (the endpoint
/// requires an API key), without actually invoking the scraper binary.
/// </summary>
[Collection(nameof(SerializedEnvVarCollection))]
public class GenericSyncEndpointEnabledTests : IAsyncLifetime
{
    private readonly PostgresLifecycleFixture _fixture;
    private ZileanWebApplicationFactory? _factory;
    private string? _savedEnableScraping;

    public GenericSyncEndpointEnabledTests(PostgresLifecycleFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        // Capture and set the env var BEFORE creating the factory so that
        // AddConfigurationFiles() picks it up during ZileanConfiguration binding.
        _savedEnableScraping = Environment.GetEnvironmentVariable("Zilean__Ingestion__EnableScraping");
        Environment.SetEnvironmentVariable("Zilean__Ingestion__EnableScraping", "true");

        _factory = new ZileanWebApplicationFactory(
            _fixture.ZileanConfiguration.Database.ConnectionString!);

        // Force host startup (CreateClient blocks until ready).
        using var client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task OnDemandGenericSync_WithScrapingEnabled_Unauthenticated_Returns401()
    {
        // Unauthenticated client (no X-API-KEY header) — the endpoint requires authorization.
        var client = _factory!.CreateClient();

        var response = await client.GetAsync("/dmm/on-demand-generic-sync");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "because the on-demand-generic-sync endpoint requires an API key when scraping is enabled");
    }

    public Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("Zilean__Ingestion__EnableScraping", _savedEnableScraping);
        _factory?.Dispose();
        return Task.CompletedTask;
    }
}