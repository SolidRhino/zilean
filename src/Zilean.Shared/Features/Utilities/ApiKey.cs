namespace Zilean.Shared.Features.Utilities;

/// <summary>
/// Provides utility methods for generating API keys.
/// </summary>
public static class ApiKey
{
    /// <summary>
    /// Generates a random API key by concatenating two GUIDs (without hyphens).
    /// </summary>
    /// <returns>A 64-character hex string suitable for use as an API key.</returns>
    public static string Generate() => $"{Guid.NewGuid():N}{Guid.NewGuid():N}";
}