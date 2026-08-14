namespace Zilean.ApiService.Features.Authentication;

/// <summary>
/// Validates the <c>X-API-KEY</c> request header against the configured API key and produces an authenticated principal on success.
/// </summary>
/// <param name="options">Monitors the <see cref="AuthenticationSchemeOptions"/> for the scheme.</param>
/// <param name="logger">The logger factory used to create loggers for the handler.</param>
/// <param name="encoder">The URL encoder for authentication header values.</param>
/// <param name="configuration">The Zilean configuration containing the expected API key.</param>
public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ZileanConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>
    /// Authenticates the current request by comparing the <c>X-API-KEY</c> header to the configured key using a fixed-time comparison.
    /// </summary>
    /// <returns>A successful <see cref="AuthenticateResult"/> with an <c>ApiKeyUser</c> principal, or a failed result when the key is missing or invalid.</returns>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-API-KEY", out var extractedApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("API Key was not provided"));
        }

        var configuredApiKey = configuration.ApiKey;
        if (string.IsNullOrEmpty(configuredApiKey) ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(extractedApiKey!),
                Encoding.UTF8.GetBytes(configuredApiKey)))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API Key"));
        }

        var claims = new[] { new Claim(ClaimTypes.Name, "ApiKeyUser") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}