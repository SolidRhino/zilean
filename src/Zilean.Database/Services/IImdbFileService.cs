namespace Zilean.Database.Services;

/// <summary>
/// Provides ingestion, storage, and search operations for IMDb metadata files.
/// </summary>
public interface IImdbFileService
{
    /// <summary>
    /// Adds an <see cref="ImdbFile"/> to the in-memory staging bag for bulk storage.
    /// </summary>
    /// <param name="imdbFile">The IMDb file entry to stage.</param>
    void AddImdbFile(ImdbFile imdbFile);

    /// <summary>
    /// Bulk-stores all staged <see cref="ImdbFile"/> entries via upsert.
    /// </summary>
    /// <returns>A task representing the asynchronous store operation.</returns>
    Task StoreImdbFiles();

    /// <summary>
    /// Searches IMDb metadata for entries matching the given query.
    /// </summary>
    /// <param name="query">The title search query.</param>
    /// <param name="year">Optional release year to filter by.</param>
    /// <param name="category">Optional IMDb category to filter by.</param>
    /// <returns>An array of matching <see cref="ImdbSearchResult"/> entries.</returns>
    Task<ImdbSearchResult[]> SearchForImdbIdAsync(string query, int? year = null, string? category = null);

    /// <summary>
    /// Persists the timestamp of the last IMDb import.
    /// </summary>
    /// <param name="imdbLastImport">The import metadata to store.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetImdbLastImportAsync(ImdbLastImport imdbLastImport);

    /// <summary>
    /// Retrieves the timestamp of the last IMDb import, if any.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The last import metadata, or <c>null</c> if no import has been recorded.</returns>
    Task<ImdbLastImport?> GetImdbLastImportAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the number of IMDb files currently staged in memory.
    /// </summary>
    int ImdbFileCount { get; }

    /// <summary>
    /// Runs VACUUM ANALYZE on the IMDb file indexes to maintain statistics.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task VaccumImdbFilesIndexes(CancellationToken cancellationToken);
}
