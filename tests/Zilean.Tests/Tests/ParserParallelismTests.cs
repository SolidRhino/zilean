using Zilean.Shared.Features.Configuration;
using Zilean.Shared.Features.Python;
using Zilean.Tests.Collections;

namespace Zilean.Tests.Tests;

[Collection(nameof(SerializedEnvVarCollection))]
public class ParserParallelismTests
{
    private const int BenchmarkBatchSize = 5000;
    private readonly ITestOutputHelper _output;

    public ParserParallelismTests(ITestOutputHelper output)
    {
        _output = output;
        Environment.SetEnvironmentVariable("ZILEAN_PYTHON_PYLIB",
            Environment.GetEnvironmentVariable("ZILEAN_PYTHON_PYLIB")
            ?? "/usr/lib/libpython3.12.so.1.0");
    }

    [Trait("Category", "RequiresPython")]
    [Trait("Category", "Benchmark")]
    [Fact]
    public async Task Benchmark_Parse5K_QuietVsVerbose()
    {
        var config = new ZileanConfiguration();
        var logger = Substitute.For<ILogger<PythonRuntimeService>>();
        var runtime = new PythonRuntimeService(logger, config);
        var service = new TorrentParser(runtime, Substitute.For<ILogger<TorrentParser>>(), config);

        try
        {
            config.Parsing.VerboseLogging = false;
            var quietMs = await TimeParse(service);

            config.Parsing.VerboseLogging = true;
            var verboseMs = await TimeParse(service);

            _output.WriteLine(
                $"[BENCHMARK] Parsed {BenchmarkBatchSize} torrents — quiet={quietMs / 1000.0:F2}s verbose={verboseMs / 1000.0:F2}s ratio={(double)verboseMs / quietMs:F2}x");
        }
        finally
        {
            await runtime.StopPythonEngine();
        }
    }
    private static async Task<long> TimeParse(TorrentParser service)
    {
        var torrents = GenerateTorrents(BenchmarkBatchSize);
        var sw = Stopwatch.StartNew();
        await service.ParseAndPopulateAsync(torrents);
        sw.Stop();
        return sw.ElapsedMilliseconds;
    }

    private static List<ExtractedDmmEntry> GenerateTorrents(int count)
    {
        var torrents = new List<ExtractedDmmEntry>();
        var random = new Random(42);
        var titles = new[]
        {
            "Iron.Man.2008.INTERNAL.REMASTERED.2160p.UHD.BluRay.X265-IAMABLE",
            "Harry.Potter.and.the.Sorcerers.Stone.2001.2160p.UHD.BluRay.X265-IAMABLE",
            "The.Dark.Knight.2008.2160p.UHD.BluRay.X265-IAMABLE",
            "Inception.2010.2160p.UHD.BluRay.X265-IAMABLE",
            "The.Matrix.1999.2160p.UHD.BluRay.X265-IAMABLE",
            "The.Witcher.S01E01.1080p.WEB.H264-METCON",
            "The.Witcher.S01E02.1080p.WEB.H264-METCON",
            "Breaking.Bad.S05E14.Ozymandias.1080p.BluRay.X264-DEMAND",
            "Pulp.Fiction.1994.REMASTERED.1080p.BluRay.X264-AMIABLE",
            "Interstellar.2014.IMAX.2160p.UHD.BluRay.X265-TERMINAL",
        };

        for (int i = 0; i < count; i++)
        {
            torrents.Add(new ExtractedDmmEntry(
                $"benchmarkhash{i:D10}",
                titles[random.Next(titles.Length)],
                (long)(random.NextDouble() * 100000000000),
                null));
        }
        return torrents;
    }
}
