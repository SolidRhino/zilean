namespace Zilean.Shared.Features.Python;

public class TorrentParser(PythonRuntimeService runtime, ILogger<TorrentParser> logger, ZileanConfiguration configuration)
{
    private readonly ILogger<TorrentParser> _logger = logger;
    private readonly ZileanConfiguration _configuration = configuration;

    private const string ParserScript =
        """
        from RTN import parse
        import asyncio
        import gc
        from rich.progress import Progress, TaskID
        from loguru import logger

        light_blue = "\033[38;2;0;175;255m"
        light_green = "\033[38;2;172;233;149m"
        red = "\033[38;2;255;0;0m"
        reset = "\033[0m"

        custom_format = (
            "[{time:HH:mm:ss}] | "
            f"{light_blue}{{level}}{reset} | "
            f"{light_green}\"Zilean.Scraper.Features.Python.ParseTorrentNameService\"{reset} | "
            "{message}"
        )

        logger.remove()
        logger.add(lambda msg: print(msg, end=''), format=custom_format, colorize=True)

        sem = None
        VERBOSE = False

        async def parse_torrent(info):
            async with sem:
                title, info_hash = info
                try:
                    result = parse(title)
                    if VERBOSE:
                        logger.info(
                           f"Title: {light_blue}{title}{reset}, "
                           f"Parsed Title: {light_green}{result.parsed_title}{reset}, "
                           f"Is Adult: {light_blue}{result.adult}{reset}, "
                           f"Is Trash: {light_blue}{result.trash}{reset}")
                    return {'infoHash': info_hash, 'result': result}
                except Exception as e:
                    logger.error(f"Failed to parse title: {red}{title}{reset}, "f"Error: {red}{e}{reset}")
                    return {'infoHash': info_hash, 'result': None, 'error': str(e)}

        def parse_torrent_single(info, verbose):
            title, info_hash = info
            try:
                result = parse(title)
                if verbose:
                    logger.info(
                       f"Title: {light_blue}{title}{reset}, "
                       f"Parsed Title: {light_green}{result.parsed_title}{reset}, "
                       f"Is Adult: {light_blue}{result.adult}{reset}, "
                       f"Is Trash: {light_blue}{result.trash}{reset}")
                return {'infoHash': info_hash, 'result': result}
            except Exception as e:
                logger.error(f"Failed to parse title: {red}{title}{reset}, "f"Error: {red}{e}{reset}")
                return {'infoHash': info_hash, 'result': None, 'error': str(e)}

        async def parse_torrents(infos, max_concurrent_tasks):
            global sem
            sem = asyncio.Semaphore(max_concurrent_tasks)
            tasks = [parse_torrent(info) for info in infos]
            results = await asyncio.gather(*tasks, return_exceptions=True)
            return results

        async def process_batches(info_batches, max_concurrent_tasks):
            results = []
            total_batches = len(info_batches)
            logger.info(f"Total batches to process: {light_blue}{total_batches}{reset}")
            logger.info(f"Max concurrent tasks: {light_green}{max_concurrent_tasks}{reset}")
            logger.info("Starting to process batches")
            for batch_number, infos in enumerate(info_batches, start=1):
                try:
                    batch_results = await parse_torrents(infos, max_concurrent_tasks)
                    results.extend(batch_results)
                    logger.info(f"Processed batch {light_blue}{batch_number}{reset}/{light_green}{total_batches}{reset}")
                except Exception as e:
                    logger.error(f"Batch {red}{batch_number}{reset} failed with error: {red}{e}{reset}")
            return results

        def run_process_batches(info_batches, max_concurrent_tasks, verbose):
            global VERBOSE
            VERBOSE = verbose
            return asyncio.run(process_batches(info_batches, max_concurrent_tasks))
        """;

    public async Task<List<TorrentInfo>> ParseAndPopulateAsync(List<ExtractedDmmEntry> torrents, int batchSize = 5000)
    {
        await runtime.Initialization;

        PyModule? scope = null;
        dynamic gc = null;
        dynamic runSingle = null;

        try
        {
            _logger.LogInformation("RTN: Parsing {Count} torrents", torrents.Count);

            var torrentDict = torrents.ToDictionary(t => t.InfoHash!, t => t);

            var infos = torrents
                .Select(x => new List<object>
                {
                    x.Filename!,
                    x.InfoHash!
                })
                .ToList();

            using (Py.GIL())
            {
                scope = Py.CreateScope();
                scope.Exec(ParserScript);
                gc = scope.Get("gc");
                runSingle = scope.Get("parse_torrent_single");
                var verbose = _configuration.Parsing.VerboseLogging;

                foreach (var info in infos)
                {
                    dynamic result = runSingle(info, verbose);
                    try
                    {
                        string infoHash = result["infoHash"].As<string>();
                        var parsedResult = result["result"];

                        if (!torrentDict.TryGetValue(infoHash, out ExtractedDmmEntry? torrent))
                        {
                            continue;
                        }

                        ParseTorrentTitleResponse parsedResponse = ParseResult(parsedResult);
                        if (!parsedResponse.Success)
                        {
                            continue;
                        }

                        parsedResponse.Response.InfoHash = torrent.InfoHash;
                        parsedResponse.Response.Size = torrent.Filesize.ToString();
                        parsedResponse.Response.RawTitle = torrent.Filename;
                        torrent.ParseResponse = parsedResponse.Response;
                    }
                    finally
                    {
                        result.Dispose();
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while parsing torrent: {Message}", e.Message);
            throw;
        }
        finally
        {
            using (Py.GIL())
            {
                runSingle?.Dispose();
                gc?.collect();
                scope?.Dispose();
            }
        }

        return torrents.Select(x => x.ParseResponse)
            .OfType<TorrentInfo>()
            .ToList();
    }

    public async Task<TorrentInfo> ParseAndPopulateTorrentInfoAsync(TorrentInfo torrent)
    {
        await runtime.Initialization;

        PyModule? scope = null;
        dynamic runSingle = null;
        dynamic gc = null;
        PyString rawTitle = null;
        PyString infoHash = null;
        PyTuple torrentParsable = null;
        dynamic result = null;

        try
        {
            using (Py.GIL())
            {
                scope = Py.CreateScope();
                scope.Exec(ParserScript);
                gc = scope.Get("gc");
                runSingle = scope.Get("parse_torrent_single");
                rawTitle = new PyString(torrent.RawTitle);
                infoHash = new PyString(torrent.InfoHash);

                torrentParsable = new PyTuple([
                    rawTitle,
                    infoHash
                ]);

                _logger.LogInformation("RTN: Parsing {Incoming}", torrent.RawTitle);

                result = runSingle(torrentParsable, _configuration.Parsing.VerboseLogging);
                var parsedResult = result["result"];
                ParseTorrentTitleResponse parsedResponse = ParseResult(parsedResult);

                if (!parsedResponse.Success)
                {
                    return torrent;
                }

                parsedResponse.Response.InfoHash = torrent.InfoHash;
                parsedResponse.Response.Size = torrent.Size;
                torrent = parsedResponse.Response;
            }

            return torrent;

        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while parsing torrent: {Message}", e.Message);
            throw;
        }
        finally
        {
            using (Py.GIL())
            {
                result?.Dispose();
                torrentParsable?.Dispose();
                rawTitle?.Dispose();
                infoHash?.Dispose();
                runSingle?.Dispose();
                gc?.collect();
                scope?.Dispose();
            }
        }
    }

    private ParseTorrentTitleResponse ParseResult(dynamic? result)
    {
        try
        {
            if (result == null)
            {
                return new ParseTorrentTitleResponse(false, null);
            }

            var json = result.model_dump_json()?.As<string?>();

            if (json is null || string.IsNullOrEmpty(json))
            {
                return new ParseTorrentTitleResponse(false, null);
            }

            var mediaType = result.GetAttr("type")?.As<string>();

            if (string.IsNullOrEmpty(mediaType))
            {
                return new ParseTorrentTitleResponse(false, null);
            }

            var torrentInfo = JsonSerializer.Deserialize<TorrentInfo>(json);

            torrentInfo.Category = CategoryClassifier.DetectCategory(torrentInfo.Extension, torrentInfo.RawTitle, torrentInfo.IsAdult, mediaType);

            result.Dispose();

            return new ParseTorrentTitleResponse(true, torrentInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while parsing result");
            return new ParseTorrentTitleResponse(false, null);
        }
    }
}