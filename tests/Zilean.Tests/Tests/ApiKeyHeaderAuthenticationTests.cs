namespace Zilean.Tests.Tests;

[Collection(nameof(ApiTestCollection))]
public class ApiKeyHeaderAuthenticationTests(PostgresLifecycleFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient();

    // A. Missing X-API-KEY header -> 401 (handler branch: missing header)

    [Fact]
    public async Task Blacklist_Add_WithoutApiKey_Returns401()
    {
        var response = await _client.PutAsync("/blacklist/add?info_hash=test&reason=test", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "because the X-API-KEY header is required");
    }

    [Fact]
    public async Task Torrents_CheckCached_WithoutApiKey_Returns401()
    {
        var response = await _client.GetAsync("/torrents/checkcached?Hashes=test");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "because the X-API-KEY header is required");
    }

    // B. Wrong X-API-KEY header -> 401 (handler branch: mismatch)

    [Fact]
    public async Task Blacklist_Add_WithWrongApiKey_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Put, "/blacklist/add?info_hash=test&reason=test");
        request.Headers.Add("X-API-KEY", "wrong-key-value");
        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "because an incorrect X-API-KEY must be rejected");
    }

    [Fact]
    public async Task Torrents_CheckCached_WithWrongApiKey_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/torrents/checkcached?Hashes=test");
        request.Headers.Add("X-API-KEY", "wrong-key-value");
        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "because an incorrect X-API-KEY must be rejected");
    }

    // C. Correct X-API-KEY header -> passes auth (handler branch: match)

    [Fact]
    public async Task Blacklist_Add_WithCorrectApiKey_PassesAuth()
    {
        var client = fixture.Factory.CreateAuthenticatedClient();
        var response = await client.PutAsync("/blacklist/add?info_hash=nonexistenthash&reason=test", null);
        // Auth passes; the hash is not already blacklisted, so AddBlacklistItem succeeds with 204.
        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "because a correct X-API-KEY must pass auth and the new hash is added successfully");
    }

    [Fact]
    public async Task Torrents_CheckCached_WithCorrectApiKey_PassesAuth()
    {
        var client = fixture.Factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/torrents/checkcached?Hashes=testhash");
        // Auth passes; a single hash under MaxHashesToCheck returns 200 with the cache-check result.
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "because a correct X-API-KEY must pass auth and a single hash returns the cached status");
    }

    // D. Empty X-API-KEY header -> 401 (handler branch: empty value)

    [Fact]
    public async Task Blacklist_Add_WithEmptyApiKey_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Put, "/blacklist/add?info_hash=test&reason=test");
        request.Headers.Add("X-API-KEY", "");
        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "because an empty X-API-KEY value must be rejected like a missing key");
    }
}