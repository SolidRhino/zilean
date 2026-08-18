using System.Net.Http.Json;
using System.Text.Json;

namespace Zilean.Tests.Tests;

/// <summary>
/// Integration tests for the anonymous /status endpoint, which exposes operational
/// metadata (database health, counts, per-category torrent counts, status). Verifies
/// the endpoint returns 200 with the expected shape when the database is reachable
/// (seeded fixture).
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

    [Fact]
    public async Task Status_Returns200_WithCategoryCounts()
    {
        var response = await _client.GetAsync("/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        content.GetProperty("categories").ValueKind.Should().Be(JsonValueKind.Object,
            "because the status response must include a categories object");

        var categories = content.GetProperty("categories");

        // Seed data: 1 movie (The Matrix), 2 tvSeries (The Witcher, Breaking Bad),
        // 1 book (Mistborn), 1 audiobook (Dune), 0 xxx.
        categories.GetProperty("movies").GetInt64().Should().Be(1,
            "because the seed data contains exactly 1 movie (The Matrix)");
        categories.GetProperty("tv").GetInt64().Should().Be(2,
            "because the seed data contains exactly 2 TV series (The Witcher, Breaking Bad)");
        categories.GetProperty("books").GetInt64().Should().Be(1,
            "because the seed data contains exactly 1 book (Mistborn)");
        categories.GetProperty("audiobooks").GetInt64().Should().Be(1,
            "because the seed data contains exactly 1 audiobook (Dune)");
        categories.GetProperty("xxx").GetInt64().Should().Be(0,
            "because the seed data contains no XXX torrents");
    }
}