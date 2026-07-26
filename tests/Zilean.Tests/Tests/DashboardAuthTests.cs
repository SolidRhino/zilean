using System.Net;
using System.Net.Http.Json;

namespace Zilean.Tests.Tests;

[Collection(nameof(ApiTestCollection))]
public class DashboardAuthTests : IAsyncLifetime
{
    private readonly PostgresLifecycleFixture _fixture;
    private ZileanWebApplicationFactory _dashboardFactory = null!;
    private HttpClient _client = null!;
    private string _apiKey = null!;

    public DashboardAuthTests(PostgresLifecycleFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        // Spin up a second factory with EnableDashboard=true, reusing the same Postgres.
        _dashboardFactory = new ZileanWebApplicationFactory(
            _fixture.ZileanConfiguration.Database.ConnectionString!,
            enableDashboard: true);

        // CreateClient blocks until the host is fully started.
        _client = _dashboardFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
        });

        // Read the API key the running app actually bound, so we use the same secret.
        using var scope = _dashboardFactory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<ZileanConfiguration>();
        _apiKey = config.ApiKey!;
    }

    [Fact]
    public async Task Login_Get_Returns200_NoRedirectLoop()
    {
        // Proves the [AllowAnonymous] on Login.razor defeats the RequireAuthorization
        // gate on MapRazorComponents — GET /login does not loop back to /login.
        var response = await _client.GetAsync("/login");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "GET /login must render the login form (AllowAnonymous), not redirect or loop");
    }

    [Fact]
    public async Task Login_Post_WrongKey_Returns401_NoCookie()
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["apiKey"] = "this-is-not-the-right-key",
        });

        var response = await _client.PostAsync("/auth/login", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "a wrong API key must not authenticate");
        response.Headers.Contains("Set-Cookie").Should().BeFalse(
            "no dashboard cookie should be issued on a failed login");
    }

    [Fact]
    public async Task Login_Post_CorrectKey_IssuesCookie_AndRedirectsToDashboard()
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["apiKey"] = _apiKey,
        });

        var response = await _client.PostAsync("/auth/login", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect,
            "a correct API key must issue a redirect to /dashboard");
        response.Headers.Location!.ToString().Should().Be("/dashboard");

        var cookie = response.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? cookies.FirstOrDefault()
            : null;
        cookie.Should().NotBeNull("the dashboard auth cookie must be set on successful login");
        cookie.Should().Contain("ZileanDashboard", "the cookie must use the configured name");
        cookie.Should().Contain("httponly", "the cookie must be HttpOnly");
    }

    [Fact]
    public async Task Dashboard_Get_WithCookie_RendersDashboard()
    {
        // Login to obtain the auth cookie, then send it back to GET /
        // to prove the cookie actually grants dashboard access.
        using var loginContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["apiKey"] = _apiKey,
        });
        var loginResponse = await _client.PostAsync("/auth/login", loginContent);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var cookieHeader = loginResponse.Headers.GetValues("Set-Cookie").First();

        // Replay the cookie on a fresh GET / request.
        using var authClient = _dashboardFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = false,
        });
        authClient.DefaultRequestHeaders.Add("Cookie", cookieHeader);

        var response = await authClient.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "with a valid dashboard cookie, / should render the dashboard, not redirect");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("zilean-logo",
            "authenticated / should render dashboard chrome (the logo is on Dashboard.razor)");
    }

    [Fact]
    public async Task Dashboard_Get_WithoutCookie_IsNotAuthorized()
    {
        // The Razor components endpoint is gated by RequireAuthorization(DashboardPolicy).
        // Without a cookie the auth fails; the AuthorizeRouteView NotAuthorized template
        // redirects to /login via RedirectToLogin. We assert the request does not return 200
        // with dashboard content (the actual status depends on the redirect mechanism —
        // 302 to /login, or 200 with redirect content). Either way, it must NOT render the
        // dashboard.
        var response = await _client.GetAsync("/");

        if (response.StatusCode == HttpStatusCode.Redirect)
        {
            // The cookie auth scheme redirects to /login?ReturnUrl=%2F (standard
            // ASP.NET Core cookie auth challenge behavior). Parse the Location to
            // assert both the path and the return URL precisely.
            var location = response.Headers.Location!;
            location.AbsolutePath.Should().Be("/login",
                "unauthenticated / should redirect to the login page");
            location.Query.Should().Contain("ReturnUrl=%2F",
                "the redirect should carry the original URL (/) for post-login return");
        }
        else
        {
            var body = await response.Content.ReadAsStringAsync();
            body.Should().NotContain("zilean-logo", "the dashboard must not render without auth");
        }
    }

    [Fact]
    public async Task Logout_WithoutCookie_RequiresAuth()
    {
        var response = await _client.PostAsync("/logout", null);

        // /logout is gated by DashboardPolicy; without a cookie it must challenge.
        response.StatusCode.Should().NotBe(HttpStatusCode.OK,
            "POST /logout without a cookie must not be reachable");
        (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Redirect)
            .Should().BeTrue("unauthenticated /logout must be challenged or redirected");
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _dashboardFactory.Dispose();
        // Clear the process-global env var so it can't leak into later factory creations.
        Environment.SetEnvironmentVariable("Zilean__EnableDashboard", null);
        return Task.CompletedTask;
    }
}