namespace Zilean.ApiService.Features.Authentication;

/// <summary>
/// Provides the authentication scheme, policy, and dashboard cookie names used by the API key and dashboard auth pipelines.
/// </summary>
public static class ApiKeyAuthentication
{
    /// <summary>
    /// The authentication scheme name for API key header authentication.
    /// </summary>
    public const string Scheme = "ApiKey";

    /// <summary>
    /// The authorization policy name requiring a valid API key.
    /// </summary>
    public const string Policy = "ApiKeyPolicy";

    /// <summary>
    /// The authentication scheme name for the dashboard cookie-based authentication.
    /// </summary>
    public const string DashboardScheme = "DashboardCookie";

    /// <summary>
    /// The authorization policy name requiring an authenticated dashboard cookie.
    /// </summary>
    public const string DashboardPolicy = "DashboardPolicy";
}