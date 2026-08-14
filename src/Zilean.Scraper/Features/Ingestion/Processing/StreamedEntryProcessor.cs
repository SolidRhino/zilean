namespace Zilean.Scraper.Features.Ingestion.Processing;

/// <summary>
/// Processes streamed entries from a generic endpoint (Zurg or another Zilean instance)
/// through a bounded channel, transforming them into torrent metadata for bulk-upsert.
/// </summary>
/// <param name="torrentInfoService">The torrent info service for bulk-upsert operations.</param>
/// <param name="parseTorrentNameService">The torrent name parser for RTN parsing.</param>
/// <param name="loggerFactory">The logger factory for creating loggers.</param>
/// <param name="clientFactory">The HTTP client factory for creating HTTP clients.</param>
/// <param name="configuration">The application configuration.</param>
public class StreamedEntryProcessor(
    ITorrentInfoService torrentInfoService,
    TorrentParser parseTorrentNameService,
    ILoggerFactory loggerFactory,
    IHttpClientFactory clientFactory,
    ZileanConfiguration configuration) : GenericProcessor<StreamedEntry>(loggerFactory, torrentInfoService, parseTorrentNameService, configuration)
{
    private GenericEndpoint? _currentEndpoint;

    /// <summary>
    /// Converts a <see cref="StreamedEntry"/> into an <see cref="ExtractedDmmEntry"/>.
    /// </summary>
    /// <param name="input">The streamed entry to convert.</param>
    /// <returns>The converted <see cref="ExtractedDmmEntry"/>.</returns>
    protected override ExtractedDmmEntry TransformToTorrent(StreamedEntry input) =>
        ExtractedDmmEntry.FromStreamedEntry(input);

    /// <summary>
    /// Processes entries from the specified generic endpoint, fetching and parsing
    /// the streamed JSON response into torrent metadata.
    /// </summary>
    /// <param name="endpoint">The generic endpoint to scrape.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task ProcessEndpointAsync(GenericEndpoint endpoint, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Processing URL: {@Url}", endpoint);
        _processedCounts.Reset();
        _currentEndpoint = endpoint;
        await ProcessAsync(ProduceEntriesAsync, cancellationToken);
        _processedCounts.WriteOutput(_configuration, sw);
        sw.Stop();
    }

    private async Task ProduceEntriesAsync(ChannelWriter<Task<StreamedEntry>> writer, CancellationToken cancellationToken)
    {
        try
        {
            if (_currentEndpoint is null)
            {
                _logger.LogError("Endpoint not set before calling ProduceEntriesAsync.");
                throw new InvalidOperationException("Endpoint not set");
            }

            var httpClient = clientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(_configuration.Ingestion.RequestTimeout);

            var fullUrl = _currentEndpoint.EndpointType switch
            {
                GenericEndpointType.Zurg => $"{_currentEndpoint.Url}{_configuration.Ingestion.ZurgEndpointSuffix}",
                GenericEndpointType.Zilean => $"{_currentEndpoint.Url}{_configuration.Ingestion.ZileanEndpointSuffix}",
                GenericEndpointType.Generic => $"{_currentEndpoint.Url}{_currentEndpoint.EndpointSuffix}",
                _ => throw new InvalidOperationException($"Unknown endpoint type: {_currentEndpoint.EndpointType}")
            };

            if (_currentEndpoint.EndpointType == GenericEndpointType.Zilean)
            {
                httpClient.DefaultRequestHeaders.Add("X-Api-Key", _currentEndpoint.ApiKey);
            }
            if (_currentEndpoint.EndpointType == GenericEndpointType.Generic)
            {
                if (!string.IsNullOrEmpty(_currentEndpoint.Authorization))
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", _currentEndpoint.Authorization);
                }
            }

            var response = await httpClient.GetAsync(fullUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<StreamedEntry>(stream, options, cancellationToken))
            {
                if (item is not null)
                {
                    await writer.WriteAsync(Task.FromResult(item), cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching and producing entries for URL: {Url}", _currentEndpoint.Url);
        }
        finally
        {
            writer.Complete();
        }
    }
}
