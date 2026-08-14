namespace Zilean.Scraper.Features.LzString;

/// <summary>
/// Caches a per-thread <see cref="StringBuilder"/> instance to reduce allocations
/// during LZ-string decompression.
/// </summary>
public static class StringBuilderCache
{
    [ThreadStatic]
    private static StringBuilder? _cachedInstance;

    /// <summary>
    /// Acquires a <see cref="StringBuilder"/>, reusing a cached instance when possible.
    /// </summary>
    /// <param name="capacity">The initial capacity hint.</param>
    /// <returns>A cleared <see cref="StringBuilder"/> ready for use.</returns>
    public static StringBuilder Acquire(int capacity = 16)
    {
        if (capacity > 360)
        {
            return new StringBuilder(capacity);
        }

        var sb = _cachedInstance;

        if (sb == null || capacity > sb.Capacity)
        {
            return new StringBuilder(capacity);
        }

        _cachedInstance = null;
        sb.Clear();

        return sb;
    }

    /// <summary>
    /// Converts the <see cref="StringBuilder"/> to a string and releases it back to the cache.
    /// </summary>
    /// <param name="sb">The <see cref="StringBuilder"/> to finalize.</param>
    /// <returns>The string content of the builder.</returns>
    public static string GetStringAndRelease(StringBuilder sb)
    {
        string result = sb.ToString();
        Release(sb);
        return result;
    }

    private static void Release(StringBuilder sb)
    {
        if (sb.Capacity <= 360)
        {
            _cachedInstance = sb;
        }
    }
}
