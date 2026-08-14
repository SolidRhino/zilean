/// <summary>
/// Adds the API key security scheme to the OpenAPI document and applies security requirements to endpoints tagged with <see cref="OpenApiSecurityMetadata"/>.
/// </summary>
public class ApiKeyDocumentTransformer : IOpenApiDocumentTransformer
{
    /// <summary>
    /// Injects the <c>X-API-KEY</c> security scheme into the document and attaches it to all protected operations.
    /// </summary>
    /// <param name="document">The OpenAPI document being generated.</param>
    /// <param name="context">The transformer context providing access to API description groups.</param>
    /// <param name="cancellationToken">A token to observe while transforming the document.</param>
    /// <returns>A completed task once the security scheme and requirements have been applied.</returns>
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        // Define the API key security scheme
        var apiKeyScheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            Name = "X-API-KEY",
            In = ParameterLocation.Header,
            Description = "API Key required for accessing protected endpoints."
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes[ApiKeyAuthentication.Scheme] = apiKeyScheme;

        foreach (var group in context.DescriptionGroups)
        {
            foreach (var apiDescription in group.Items)
            {
                var metadata = apiDescription.ActionDescriptor.EndpointMetadata?
                    .OfType<OpenApiSecurityMetadata>()
                    .FirstOrDefault();

                if (metadata is { SecurityScheme: ApiKeyAuthentication.Scheme })
                {
                    var route = apiDescription.RelativePath;
                    if (document.Paths.TryGetValue("/" + route, out var pathItem))
                    {
                        foreach (var operation in pathItem.Operations.Values)
                        {
                            operation.Security ??= [];
                            operation.Security.Add(new OpenApiSecurityRequirement
                            {
                                [apiKeyScheme] = Array.Empty<string>()
                            });
                        }
                    }
                }
            }
        }

        return Task.CompletedTask;
    }
}