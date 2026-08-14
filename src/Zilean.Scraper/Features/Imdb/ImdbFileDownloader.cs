namespace Zilean.Scraper.Features.Imdb;

/// <summary>
/// Downloads IMDb metadata files (title.basics.tsv) from the IMDb datasets endpoint.
/// </summary>
/// <param name="logger">The logger for diagnostic output.</param>
public class ImdbFileDownloader(ILogger<ImdbFileDownloader> logger)
{
    private static readonly string _dataFilePath = Path.Combine(AppContext.BaseDirectory, "data", TitleBasicsFileName);
    private const string TitleBasicsFileName = "title.basics.tsv";
    private const string ImdbDataBaseAddress = "https://datasets.imdbws.com/";

    /// <summary>
    /// Downloads the IMDb title.basics.tsv metadata file, reusing a cached copy
    /// if it is less than 30 days old.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The path to the downloaded or cached metadata file.</returns>
    public async Task<string> DownloadMetadataFile(CancellationToken cancellationToken) =>
        await DownloadFileToTempPath(TitleBasicsFileName, cancellationToken);

    private async Task<string> DownloadFileToTempPath(string fileName, CancellationToken cancellationToken)
    {
        if (File.Exists(_dataFilePath))
        {
            var fileInfo = new FileInfo(_dataFilePath);
            if (fileInfo.CreationTimeUtc <= DateTime.UtcNow.AddDays(30))
            {
                logger.LogInformation("IMDB data '{Filename}' already exists at {TempFile}. Will use records in that for import.", fileName, _dataFilePath);
                return _dataFilePath;
            }

            logger.LogInformation("IMDB data '{Filename}' is older than 30 days, deleting", fileName);
            File.Delete(_dataFilePath);
        }

        logger.LogInformation("Downloading IMDB data '{Filename}'", fileName);

        var client = CreateHttpClient();
        var response = await client.GetAsync($"{fileName}.gz", cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
        await using var fileStream = File.Create(_dataFilePath);

        await gzipStream.CopyToAsync(fileStream, cancellationToken);

        logger.LogInformation("Downloaded IMDB data '{Filename}' to {TempFile}", fileName, _dataFilePath);

        fileStream.Close();
        return _dataFilePath;
    }

    private static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(ImdbDataBaseAddress),
            Timeout = TimeSpan.FromMinutes(30),
        };

        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("curl/7.54");
        return httpClient;
    }
}
