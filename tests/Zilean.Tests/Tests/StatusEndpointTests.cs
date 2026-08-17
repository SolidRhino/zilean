using System.Net.Http.Json;
using System.Text.Json;

namespace Zilean.Tests.Tests;

/// <summary>
/// Integration tests for the anonymous /status endpoint, which exposes operational
/// metadata (database health, counts, status). Verifies the endpoint returns 200
/// with the expected shape when the database is reachable (seeded fixture).
/// </summary>
[Collection(nameof(ApiTestCollection))]
public class StatusEndpointTests
{
    private readonly HttpClient _client;

    public StatusEndpointTests(PostgresLifecycleFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task Status_Returns200_WithCountsAndStatus()
    {
        var response = await _client.GetAsync("/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("status").GetString().Should().BeOneOf("healthy", "degraded");
        content.GetProperty("database").GetBoolean().Should().BeTrue();
        content.GetProperty("counts").GetProperty("torrents").GetInt64().Should().BeGreaterThan(0);
    }
}