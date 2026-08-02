using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Zilean.Database.Services;

namespace Zilean.Tests.Tests;

/// <summary>
/// Integration tests for Torznab error paths and DB-down graceful degradation.
/// Torznab + /dmm/* endpoints are AllowAnonymous, so the default factory client works.
/// DB-down tests use <see cref="WebApplicationFactory{TEntryPoint}.WithWebHostBuilder"/>
/// to override <see cref="ITorrentInfoService"/> with a throwing stub, simulating a
/// DB failure without mutating the shared DbContextFactory's connection string
/// (which is captured at registration time and cannot be changed post-startup).
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
        var throwingClient = CreateClientWithThrowingTorrentInfoService();

        var response = await throwingClient.GetAsync("/torznab/api?t=search&q=The%20Matrix");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "because a DB exception during search is caught and returned as error 900");

        var body = await response.Content.ReadAsStringAsync();
        var doc = XDocument.Parse(body);
        doc.Root!.Name.LocalName.Should().Be("error",
            "because the torznab handler converts DB exceptions to <error> XML");
        doc.Root.Attribute("code")!.Value.Should().Be("900",
            "because DB failures map to error code 900");
    }

    [Fact]
    public async Task Dmm_Filtered_DbDown_Returns200EmptyArray()
    {
        var throwingClient = CreateClientWithThrowingTorrentInfoService();

        var response = await throwingClient.GetAsync("/dmm/filtered?Query=The%20Matrix");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "because PerformFilteredSearch has a catch-all returning 200");

        var body = await response.Content.ReadAsStringAsync();
        body.Trim().Should().Be("[]",
            "because the catch-all returns an empty array on DB failure");
    }

    [Fact]
    public async Task Dmm_Search_DbDown_Returns200EmptyArray()
    {
        var throwingClient = CreateClientWithThrowingTorrentInfoService();

        var response = await throwingClient.PostAsync(
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

    /// <summary>
    /// Creates a derived factory/client where <see cref="ITorrentInfoService"/> throws on
    /// every search call, simulating a DB-down scenario. The DbContextFactory's connection
    /// string is captured at registration and cannot be mutated post-startup, so we override
    /// the service itself instead.
    /// </summary>
    private HttpClient CreateClientWithThrowingTorrentInfoService()
    {
        var throwingService = Substitute.For<ITorrentInfoService>();
        throwingService
            .When(x => x.SearchForTorrentInfoFiltered(Arg.Any<TorrentInfoFilter>(), Arg.Any<int?>()))
            .Do(_ => throw new InvalidOperationException("Simulated DB failure"));
        throwingService
            .When(x => x.SearchForTorrentInfoByOnlyTitle(Arg.Any<string>()))
            .Do(_ => throw new InvalidOperationException("Simulated DB failure"));

        return _fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ITorrentInfoService>();
                services.AddSingleton(throwingService);
            });
        }).CreateClient();
    }
}