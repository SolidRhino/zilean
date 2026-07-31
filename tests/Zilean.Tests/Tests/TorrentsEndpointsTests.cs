using System.Net.Http.Json;
using System.Text.Json;

namespace Zilean.Tests.Tests;

/// <summary>
/// Integration tests for /torrents/checkcached and /torrents/all. These endpoints
/// require API key auth (use CreateAuthenticatedClient). The /torrents/all test
/// proves the long.Parse(Size) fix: seeded Matrix data has Size="15.5 GB" which would
/// crash long.Parse before the fix, truncating the JSON stream.
/// </summary>
[Collection(nameof(ApiTestCollection))]
public class TorrentsEndpointsTests
{
    private readonly HttpClient _client;

    private const string MatrixHash = "aabbccdd00112233aabb00112233aabbccdd0011";

    public TorrentsEndpointsTests(PostgresLifecycleFixture fixture)
    {
        _client = fixture.Factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task CheckCached_WithEmptyHashes_Returns400()
    {
        var response = await _client.GetAsync("/torrents/checkcached?Hashes=");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "because an empty Hashes query is rejected with 400");

        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("message").GetString().Should().Be(
            "No hashes provided",
            "because the ErrorResponse message for empty hashes is 'No hashes provided'");
    }

    [Fact]
    public async Task CheckCached_WithTooManyHashes_Returns400()
    {
        // 101 dummy hashes → exceeds MaxHashesToCheck (100).
        var hashes = string.Join(",", Enumerable.Range(0, 101).Select(i => $"hash{i:D20}"));
        var response = await _client.GetAsync($"/torrents/checkcached?Hashes={hashes}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "because exceeding MaxHashesToCheck (100) is rejected with 400");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Too many hashes provided. The limit is 100.",
            "because the ErrorResponse message includes the max limit");
    }

    [Fact]
    public async Task CheckCached_WithValidCachedHash_Returns200_IsCachedTrue()
    {
        var response = await _client.GetAsync($"/torrents/checkcached?Hashes={MatrixHash}");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "because a valid single-hash check returns 200");

        var body = await response.Content.ReadAsStringAsync();
        var items = JsonDocument.Parse(body).RootElement;

        items.GetArrayLength().Should().Be(1,
            "because one hash was queried");
        var item = items[0];
        item.GetProperty("is_cached").GetBoolean().Should().BeTrue(
            "because the Matrix hash exists in the seeded DB");
        item.GetProperty("info_hash").GetString().Should().Be(MatrixHash,
            "because the response echoes the queried hash");
        item.GetProperty("item").ValueKind.Should().NotBe(JsonValueKind.Null,
            "because a cached item includes the torrent record");
    }

    [Fact]
    public async Task CheckCached_WithUnknownHash_Returns200_IsCachedFalse()
    {
        var unknownHash = "unknownhash0001unknownhash0001unknownhash001".PadRight(40, '0')[..40];

        var response = await _client.GetAsync($"/torrents/checkcached?Hashes={unknownHash}");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "because an unknown hash still returns 200");

        var body = await response.Content.ReadAsStringAsync();
        var items = JsonDocument.Parse(body).RootElement;

        items.GetArrayLength().Should().Be(1,
            "because one hash was queried");
        var item = items[0];
        item.GetProperty("is_cached").GetBoolean().Should().BeFalse(
            "because the hash does not exist in the seeded DB");
        item.GetProperty("info_hash").GetString().Should().Be(unknownHash,
            "because the response echoes the queried hash");
        item.GetProperty("item").ValueKind.Should().Be(JsonValueKind.Null,
            "because an uncached item has no torrent record");
    }

    [Fact]
    public async Task CheckCached_WithMixedHashes_ReturnsBothCachedAndUncached()
    {
        var unknownHash = new string('z', 40);
        var response = await _client.GetAsync($"/torrents/checkcached?Hashes={MatrixHash},{unknownHash}");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "because a mixed-hash check returns 200");
        var body = await response.Content.ReadAsStringAsync();
        var items = JsonDocument.Parse(body).RootElement;

        items.GetArrayLength().Should().Be(2,
            "because two hashes were queried");

        var enumerated = items.EnumerateArray().ToList();
        var cachedItem = enumerated.FirstOrDefault(i =>
            i.GetProperty("is_cached").GetBoolean() &&
            i.GetProperty("info_hash").GetString() == MatrixHash);
        cachedItem.ValueKind.Should().NotBe(JsonValueKind.Undefined,
            "because the Matrix hash should be cached");

        var uncachedItem = enumerated.FirstOrDefault(i =>
            !i.GetProperty("is_cached").GetBoolean() &&
            i.GetProperty("info_hash").GetString() == unknownHash);
        uncachedItem.ValueKind.Should().NotBe(JsonValueKind.Undefined,
            "because the unknown hash should not be cached");
    }

    [Fact]
    public async Task StreamAll_Returns200JsonArray_WithSeedRows()
    {
        // This test fails before the long.Parse(Size) fix: the Matrix seed has
        // Size="15.5 GB" which crashes long.Parse mid-stream, truncating the JSON array.
        var response = await _client.GetAsync("/torrents/all");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "because the /all endpoint streams a 200 JSON array");

        var body = await response.Content.ReadAsStringAsync();
        var items = JsonDocument.Parse(body).RootElement;

        items.ValueKind.Should().Be(JsonValueKind.Array,
            "because /torrents/all emits a JSON array");
        items.GetArrayLength().Should().BeGreaterThanOrEqualTo(5,
            "because the fixture seeds at least 5 rows");

        var matrix = items.EnumerateArray().FirstOrDefault(e =>
            e.TryGetProperty("hash", out var h) && h.GetString() == MatrixHash);
        matrix.ValueKind.Should().NotBe(JsonValueKind.Undefined,
            "because the seeded Matrix torrent must appear in the stream");

        matrix.GetProperty("name").GetString().Should().Be(
            "The.Matrix.1999.2160p.UHD.BluRay.X265-IAMABLE",
            "because the Matrix seed row's RawTitle is the stream name");
        matrix.GetProperty("size").GetInt64().Should().Be(15,
            "because the seed Size '15.5 GB' is parsed via leading digits to 15, proving the TryParse fix");
    }
}