namespace Zilean.Shared.Extensions;

/// <summary>
/// Provides extension methods for JSON serialization.
/// </summary>
public static class JsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        NumberHandling = JsonNumberHandling.Strict,
    };

    /// <summary>
    /// Serializes the specified object to a JSON string using camelCase naming and cycle-safe reference handling.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>A JSON string representation of <paramref name="obj"/>.</returns>
    public static string AsJson<T>(this T obj) => JsonSerializer.Serialize(obj, _jsonSerializerOptions);
}
