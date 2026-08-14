namespace Zilean.Shared.Extensions;

/// <summary>
/// Provides extension methods for dictionary creation.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// Projects a sequence into a <see cref="ConcurrentDictionary{TKey, TValue}"/> using key and value selectors.
    /// </summary>
    /// <typeparam name="TSource">The element type of the source sequence.</typeparam>
    /// <typeparam name="TKey">The key type for the resulting dictionary.</typeparam>
    /// <typeparam name="TValue">The value type for the resulting dictionary.</typeparam>
    /// <param name="source">The source sequence to project.</param>
    /// <param name="keySelector">A function that extracts the key from each element.</param>
    /// <param name="valueSelector">A function that extracts the value from each element.</param>
    /// <returns>A <see cref="ConcurrentDictionary{TKey, TValue}"/> populated from <paramref name="source"/>.</returns>
    public static ConcurrentDictionary<TKey, TValue> ToConcurrentDictionary<TSource, TKey, TValue>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TValue> valueSelector) where TKey : notnull
    {
        var concurrentDictionary = new ConcurrentDictionary<TKey, TValue>();

        foreach (var element in source)
        {
            concurrentDictionary.TryAdd(keySelector(element), valueSelector(element));
        }

        return concurrentDictionary;
    }
}
