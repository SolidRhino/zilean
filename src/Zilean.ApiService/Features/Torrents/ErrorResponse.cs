namespace Zilean.ApiService.Features.Torrents;

/// <summary>
/// A standard error response payload with a single message field.
/// </summary>
/// <param name="message">The error message.</param>
public class ErrorResponse(string message)
{
    /// <summary>
    /// The error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; } = message;
}