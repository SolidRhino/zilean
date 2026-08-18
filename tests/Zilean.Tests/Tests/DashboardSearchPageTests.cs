using System.Net;

namespace Zilean.Tests.Tests;

/// <summary>
/// Integration tests for dashboard pages added by the features plan: the title search page
/// (/search) and the status page (/dashboard/status) with the Sync Now button and category
/// count cards. Uses a dashboard-enabled factory (like DashboardAuthTests) and authenticates
/// via the login flow to verify the pages render expected content.
/// </summary>
[Collection(nameof(ApiTestCollection))]
public class DashboardSearchPageTests : IAsyncLifetime
{
    private readonly PostgresLifecycleFixture _fixture;
    private ZileanWebApplicationFactory _dashboardFactory = null!;
    private HttpClient _client = null!;
    private string _apiKey = null!;

    public DashboardSearchPageTests(PostgresLifecycleFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _dashboardFactory = new ZileanWebApplicationFactory(
            _fixture.ZileanConfiguration.Database.ConnectionString!,
            enableDashboard: true);

        _client = _dashboardFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
        });

        using var scope = _dashboardFactory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<ZileanConfiguration>();
        _apiKey = config.ApiKey!;
    }

    [Fact]
    public async Task SearchPage_Get_WithCookie_RendersSearchForm()
    {
        using var authClient = await CreateAuthenticatedClientAsync();

        var response = await authClient.GetAsync("/search");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "with a valid dashboard cookie, /search should render the search page");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Torrent Search",
            "because the Search page heading must be rendered");
        body.Should().Contain("Enter title",
            "because the search input placeholder must be present");
    }

    [Fact]
    public async Task StatusPage_Get_WithCookie_RendersSyncNowAndCategoryCards()
    {
        using var authClient = await CreateAuthenticatedClientAsync();

        var response = await authClient.GetAsync("/dashboard/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "with a valid dashboard cookie, /dashboard/status should render the status page");
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Sync Now",
            "because the Sync Now button must be rendered when sync is not running");

        // All 5 category card headers must appear in the prerendered HTML.
        body.Should().Contain("Movies",
            "because the Movies category card must be rendered");
        body.Should().Contain("TV",
            "because the TV category card must be rendered");
        body.Should().Contain("Books",
            "because the Books category card must be rendered");
        body.Should().Contain("Audiobooks",
            "because the Audiobooks category card must be rendered");
        body.Should().Contain("XXX",
            "because the XXX category card must be rendered");
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        using var loginContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["apiKey"] = _apiKey,
        });
        var loginResponse = await _client.PostAsync("/auth/login", loginContent);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var cookieHeader = loginResponse.Headers.GetValues("Set-Cookie").First();

        var authClient = _dashboardFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = false,
        });
        authClient.DefaultRequestHeaders.Add("Cookie", cookieHeader);
        return authClient;
    }

    public Task DisposeAsync()
    {
        _dashboardFactory.Dispose();
        return Task.CompletedTask;
    }
}