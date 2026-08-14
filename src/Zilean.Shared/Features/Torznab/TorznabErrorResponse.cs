namespace Zilean.Shared.Features.Torznab;

/// <summary>
/// Generates Torznab error response XML for returning errors to *arr clients.
/// </summary>
public static class TorznabErrorResponse
{
    /// <summary>
    /// Creates a Torznab error response XML string with the given code and description.
    /// </summary>
    /// <param name="code">The numeric Torznab error code.</param>
    /// <param name="description">The human-readable error description.</param>
    /// <returns>The error XML document as a string.</returns>
    public static string Create(int code, string description)
    {
        var xdoc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("error",
                new XAttribute("code", code.ToString()),
                new XAttribute("description", description)
            )
        );

        return xdoc.Declaration + Environment.NewLine + xdoc;
    }
}
