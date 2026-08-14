namespace Zilean.Scraper.Features.Imdb;

/// <summary>
/// Imports IMDb basics TSV data, filtering to required categories and storing
/// the parsed records via the IMDb file service.
/// </summary>
/// <param name="logger">The logger for diagnostic output.</param>
/// <param name="imdbFileService">The IMDb file service for storing parsed records.</param>
public class ImdbFileProcessor(ILogger<ImdbFileProcessor> logger, IImdbFileService imdbFileService)
{
    private static readonly List<string> _requiredCategories = [
        "movie",
        "tvMovie",
        "tvSeries",
        "tvShort",
        "tvMiniSeries",
        "tvSpecial",
    ];

    /// <summary>
    /// Imports the IMDb basics TSV file, filtering entries to required categories
    /// and bulk-storing the parsed records.
    /// </summary>
    /// <param name="fileName">The path to the IMDb TSV file to import.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task Import(string fileName, CancellationToken cancellationToken)
    {
        logger.LogInformation("Importing Downloaded IMDB Basics data from {FilePath}", fileName);

        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = "\t",
            BadDataFound = null,
            MissingFieldFound = null,
            HasHeaderRecord = true,
            ShouldSkipRecord = record => !_requiredCategories.Contains(record.Row.GetField(1))
        };

        using var reader = new StreamReader(fileName);
        using var csv = new CsvReader(reader, csvConfig);

        // skip header...
        await csv.ReadAsync();

        await ReadBasicEntries(csv, imdbFileService, cancellationToken);

        await imdbFileService.StoreImdbFiles();

        await imdbFileService.VaccumImdbFilesIndexes(cancellationToken);
    }

    private static async Task ReadBasicEntries(CsvReader csv, IImdbFileService imdbFileService, CancellationToken cancellationToken)
    {
        while (await csv.ReadAsync())
        {
            var isAdultSet = int.TryParse(csv.GetField(4), out var adult);
            var yearField = csv.GetField(5);
            var isYearValid = int.TryParse(yearField == @"\N" ? "0" : yearField, out var year);

            var movieData = new ImdbFile
            {
                ImdbId = csv.GetField(0),
                Category = csv.GetField(1),
                Title = csv.GetField(2),
                Adult = isAdultSet && adult == 1,
                Year = isYearValid ? year : 0
            };

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            imdbFileService.AddImdbFile(movieData);
        }
    }
}
