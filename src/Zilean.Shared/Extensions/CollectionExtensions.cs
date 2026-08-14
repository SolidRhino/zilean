namespace Zilean.Shared.Extensions;

/// <summary>
/// Provides extension methods for chunking collections into batches.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Splits a list into chunks of the specified batch size.
    /// </summary>
    /// <typeparam name="TType">The element type of the collection.</typeparam>
    /// <param name="collection">The source list to chunk.</param>
    /// <param name="batchSize">The maximum number of elements per chunk.</param>
    /// <returns>An enumerable of lists, each containing up to <paramref name="batchSize"/> elements.</returns>
    public static IEnumerable<List<TType>> ToChunks<TType>(this List<TType> collection, int batchSize)
    {
        for (int i = 0; i < collection.Count; i += batchSize)
        {
            yield return collection.GetRange(i, Math.Min(batchSize, collection.Count - i));
        }
    }

    /// <summary>
    /// Asynchronously splits an async enumerable into chunks of the specified size.
    /// </summary>
    /// <typeparam name="T">The element type of the async source.</typeparam>
    /// <param name="source">The async source sequence to chunk.</param>
    /// <param name="size">The maximum number of elements per chunk. Must be greater than zero.</param>
    /// <param name="cancellationToken">Token to cancel enumeration.</param>
    /// <returns>An async enumerable of lists, each containing up to <paramref name="size"/> elements.</returns>
    public static async IAsyncEnumerable<List<T>> ToChunksAsync<T>(this IAsyncEnumerable<T> source, int size,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (size <= 0)
        {
            throw new ArgumentException("Chunk size must be greater than zero.", nameof(size));
        }

        var batch = new List<T>(size);

        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            batch.Add(item);
            if (batch.Count != size)
            {
                continue;
            }

            yield return batch;
            batch = new List<T>(size);
        }

        if (batch.Count > 0)
        {
            yield return batch;
        }
    }
}
