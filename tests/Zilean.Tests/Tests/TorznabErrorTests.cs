using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Zilean.Tests.Tests;

/// <summary>
/// Integration tests for Torznab error paths and DB-down graceful degradation.
/// Torznab + /dmm/* endpoints are AllowAnonymous, so the default factory client works.
/// DB-down tests mutate the ZileanConfiguration singleton's ConnectionString (read
/// per-call by Dapper) and restore it in finally to avoid leaking the bad CS to
/// collection-mates.
/// </summary>
[Collection(nameof(ApiTestCollection))]
public class TorznabErrorTests
{
    private readonly HttpClient _client;
    private readonly PostgresLifecycleFixture _fixture;

    public TorznabErrorTests(PostgresLifecycleFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    private const string BadConnectionString =
        "Host=127.0.0.1;Port=1;Database=zilean;Username=postgres;Password=postgres;Timeout=2";

    [Fact]
    public async Task Torznab_Search_LimitExceedsMax_ReturnsError900()
    {
        // LimitsMax = 5000; requesting 5001 must be rejected before the DB is queried.
        var response = await _client.GetAsync("/torznab/api?t=search&q=x&limit=5001");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "because a limit exceeding LimitsMax (5000) is a client error");

        var body = await response.Content.ReadAsStringAsync();
        var doc = XDocument.Parse(body);
        doc.Root!.Name.LocalName.Should().Be("error",
            "because the torznab error response is an <error> element");
        doc.Root.Attribute("code")!.Value.Should().Be("900",
            "because limit validation returns error code 900");
        doc.Root.Attribute("description")!.Value.Should().Contain("5000",
            "because the error description includes the max allowed limit");
    }

    [Fact]
    public async Task Torznab_Search_InvalidCatConversion_ReturnsError900()
    {
        // cat=abc → int.Parse throws → ToTorznabQuery returns null → error 900.
        var response = await _client.GetAsync("/torznab/api?t=search&q=x&cat=abc");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "because an invalid category conversion is a client error");

        var body = await response.Content.ReadAsStringAsync();
        var doc = XDocument.Parse(body);
        doc.Root!.Name.LocalName.Should().Be("error",
            "because the invalid-query-conversion path returns an <error> element");
        doc.Root.Attribute("code")!.Value.Should().Be("900",
            "because ToTorznabQuery returning null maps to error 900");
        doc.Root.Attribute("description")!.Value.Should().Contain("Invalid query conversion",
            "because the error description is 'Invalid query conversion'");
    }

    [Fact]
    public async Task Torznab_Search_DbDown_ReturnsError900Xml()
    {
        var config = ResolveConfig();
        var originalCs = config.Database.ConnectionString;
        try
        {
            config.Database.ConnectionString = BadConnectionString;

            var response = await _client.GetAsync("/torznab/api?t=search&q=The%20Matrix");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                "because a DB exception during search is caught and returned as error 900");

            var body = await response.Content.ReadAsStringAsync();
            var doc = XDocument.Parse(body);
            doc.Root!.Name.LocalName.Should().Be("error",
                "because the torznab handler converts DB exceptions to <error> XML");
            doc.Root.Attribute("code")!.Value.Should().Be("900",
                "because DB failures map to error code 900");
        }
        finally
        {
            config.Database.ConnectionString = originalCs;
        }
    }

    [Fact]
    public async Task Dmm_Filtered_DbDown_Returns200EmptyArray()
    {
        var config = ResolveConfig();
        var originalCs = config.Database.ConnectionString;
        try
        {
            config.Database.ConnectionString = BadConnectionString;

            var response = await _client.GetAsync("/dmm/filtered?Query=The%20Matrix");

            response.StatusCode.Should().Be(HttpStatusCode.OK,
                "because PerformFilteredSearch has a catch-all returning 200");

            var body = await response.Content.ReadAsStringAsync();
            body.Trim().Should().Be("[]",
                "because the catch-all returns an empty array on DB failure");
        }
        finally
        {
            config.Database.ConnectionString = originalCs;
        }
    }

    [Fact]
    public async Task Dmm_Search_DbDown_Returns200EmptyArray()
    {
        var config = ResolveConfig();
        var originalCs = config.Database.ConnectionString;
        try
        {
            config.Database.ConnectionString = BadConnectionString;

            var response = await _client.PostAsync(
                "/dmm/search",
                new StringContent(
                    """{"QueryText":"The Matrix"}""",
                    Encoding.UTF8,
                    "application/json"));

            response.StatusCode.Should().Be(HttpStatusCode.OK,
                "because PerformSearch has a catch-all returning 200");

            var body = await response.Content.ReadAsStringAsync();
            body.Trim().Should().Be("[]",
                "because the catch-all returns an empty array on DB failure");
        }
        finally
        {
            config.Database.ConnectionString = originalCs;
        }
    }

    private ZileanConfiguration ResolveConfig()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ZileanConfiguration>();
    }
}