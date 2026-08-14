namespace Zilean.ApiService.Features.Authentication;

/// <summary>
/// Marks an endpoint with the OpenAPI security scheme that applies to it, used by <see cref="ApiKeyDocumentTransformer"/> to annotate operations.
/// </summary>
/// <param name="securityScheme">The security scheme name associated with the endpoint.</param>
public class OpenApiSecurityMetadata(string securityScheme)
{
    /// <summary>
    /// The security scheme name applied to the endpoint.
    /// </summary>
    public string SecurityScheme { get; } = securityScheme;
}