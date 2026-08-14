namespace Zilean.ApiService.Features.Torznab;

/// <summary>
/// Provides a pooled <see cref="RecyclableMemoryStreamManager"/> for XML serialization streams.
/// </summary>
public static class StreamManager
{
    /// <summary>
    /// The shared <see cref="RecyclableMemoryStreamManager"/> instance.
    /// </summary>
    public static RecyclableMemoryStreamManager Instance { get; } = new();
}

/// <summary>
/// An <see cref="IResult"/> that serializes its content as an XML HTTP response.
/// String results are written verbatim; other types are serialized with <see cref="XmlSerializer"/>.
/// </summary>
/// <typeparam name="T">The type of the result payload.</typeparam>
/// <param name="result">The payload to serialize.</param>
/// <param name="statusCode">The HTTP status code to set on the response.</param>
public class XmlResult<T>(T result, int statusCode) : IResult
{
    private static readonly XmlSerializer _serializer = new(typeof(T));

    /// <summary>
    /// Writes the XML response to the supplied <paramref name="httpContext"/>.
    /// </summary>
    /// <param name="httpContext">The HTTP context for the current request.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.ContentType = "application/xml";
        httpContext.Response.StatusCode = statusCode;

        if (result is string xmlString)
        {
            // Handle string results directly to avoid wrapping in a <string> tag
            await httpContext.Response.WriteAsync(xmlString);
        }
        else
        {
            // Serialize non-string objects to XML
            await using var ms = StreamManager.Instance.GetStream();
            _serializer.Serialize(ms, result);

            ms.Position = 0;
            await ms.CopyToAsync(httpContext.Response.Body);
        }
    }
}