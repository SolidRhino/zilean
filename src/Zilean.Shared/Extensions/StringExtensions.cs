namespace Zilean.Shared.Extensions;

/// <summary>
/// Provides extension methods for string comparison and validation.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Determines whether the source string contains the specified substring, ignoring case.
    /// </summary>
    /// <param name="source">The source string to search within.</param>
    /// <param name="toCheck">The substring to locate.</param>
    /// <returns><c>true</c> if <paramref name="source"/> contains <paramref name="toCheck"/>; otherwise, <c>false</c>.</returns>
    public static bool ContainsIgnoreCase(this string? source, string toCheck) =>
        source.Contains(toCheck, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determines whether any string in the source collection contains the specified substring, ignoring case.
    /// </summary>
    /// <param name="source">The collection of strings to search within.</param>
    /// <param name="toCheck">The substring to locate.</param>
    /// <returns><c>true</c> if any element in <paramref name="source"/> contains
    /// <paramref name="toCheck"/>; otherwise, <c>false</c>.</returns>
    public static bool ContainsIgnoreCase(this IEnumerable<string>? source, string toCheck) =>
        source.Any(s => s.Contains(toCheck, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Determines whether the source string is null, empty, or consists only of whitespace.
    /// </summary>
    /// <param name="source">The string to test.</param>
    /// <returns><c>true</c> if <paramref name="source"/> is null, empty, or whitespace-only; otherwise, <c>false</c>.</returns>
    public static bool IsNullOrWhiteSpace(this string? source) =>
        string.IsNullOrWhiteSpace(source);
}
