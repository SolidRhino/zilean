using System.Net.Http;
using Zilean.Database.Services;
using Zilean.Scraper.Features.Ingestion.Processing;
using Zilean.Shared.Features.Python;
using Zilean.Tests.Collections;

namespace Zilean.Tests.Tests;
/// <summary>
/// Unit tests for the generic ingestion producer path (URL/header construction per
/// GenericEndpointType + exception-swallow loop). These exercise
/// StreamedEntryProcessor.ProcessEndpointAsync against a fake HTTP source without
/// requiring Python or a database. The consumer/parse path is not exercised (empty JSON
/// arrays keep the consumer's batch processor idle); it is covered conceptually by
/// ParserParallelismTests and the benchmark.
/// </summary>
[Collection(nameof(SerializedEnvVarCollection))]
public class IngestionPipelineTests
{
    private sealed class RecordingHandler : DelegatingHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public int CallCount { get; private set; }

        private readonly HttpStatusCode _status;
        private readonly string _content;

        public RecordingHandler(HttpStatusCode status, string content)
        {
            _status = status;
            _content = content;
            InnerHandler = new HttpClientHandler();
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            CallCount++;
            var response = new HttpResponseMessage(_status)
            {
                Content = new StringContent(_content, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    private static StreamedEntryProcessor CreateProcessor(
        RecordingHandler handler,
        ZileanConfiguration configuration)
    {
        var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient().Returns(new HttpClient(handler));

        var torrentInfoService = Substitute.For<ITorrentInfoService>();
        torrentInfoService
            .GetBlacklistedItems()
            .Returns(Task.FromResult(new HashSet<string>(StringComparer.OrdinalIgnoreCase)));

        // Construct a real PythonRuntimeService that faults deterministically (no native
        // pythonnet init) by temporarily unsetting ZILEAN_PYTHON_PYLIB. The parser is never
        // called for empty-array producer responses, so its faulted state is irrelevant.
        var savedPython = Environment.GetEnvironmentVariable("ZILEAN_PYTHON_PYLIB");
        Environment.SetEnvironmentVariable("ZILEAN_PYTHON_PYLIB", "");
        try
        {
            var runtime = new PythonRuntimeService(
                Substitute.For<ILogger<PythonRuntimeService>>(),
                new ZileanConfiguration());
            var parser = new TorrentParser(
                runtime,
                Substitute.For<ILogger<TorrentParser>>(),
                configuration);
            var loggerFactory = Substitute.For<ILoggerFactory>();

            return new StreamedEntryProcessor(
                torrentInfoService,
                parser,
                loggerFactory,
                clientFactory,
                configuration);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ZILEAN_PYTHON_PYLIB", savedPython);
        }
    }

    private static ZileanConfiguration DefaultConfig() => new()
    {
        Ingestion =
        {
            ZurgEndpointSuffix = "/debug/torrents",
            ZileanEndpointSuffix = "/torrents/all",
        },
        Parsing =
        {
            BatchSize = 100,
        },
    };

    [Fact]
    public async Task Zurg_AppendsZurgEndpointSuffix_NoAuthHeader()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "[]");
        var config = DefaultConfig();
        var processor = CreateProcessor(handler, config);

        var endpoint = new GenericEndpoint
        {
            Url = "http://test-zurg.local",
            EndpointType = GenericEndpointType.Zurg,
        };

        await processor.ProcessEndpointAsync(endpoint, CancellationToken.None);

        handler.CallCount.Should().Be(1, "because the producer should issue exactly one HTTP request");
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri.Should().Be(
            new Uri($"http://test-zurg.local{config.Ingestion.ZurgEndpointSuffix}"),
            "because Zurg endpoints must append the ZurgEndpointSuffix");
        handler.LastRequest.Headers.Contains("X-Api-Key").Should().BeFalse(
            "because Zurg endpoints do not authenticate");
        handler.LastRequest.Headers.Contains("Authorization").Should().BeFalse(
            "because Zurg endpoints do not authenticate");
    }

    [Fact]
    public async Task Zilean_AppendsZileanEndpointSuffix_AddsXApiKeyHeader()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "[]");
        var config = DefaultConfig();
        var processor = CreateProcessor(handler, config);

        var endpoint = new GenericEndpoint
        {
            Url = "http://test-zilean.local",
            EndpointType = GenericEndpointType.Zilean,
            ApiKey = "secret-key-123",
        };

        await processor.ProcessEndpointAsync(endpoint, CancellationToken.None);

        handler.CallCount.Should().Be(1, "because the producer should issue exactly one HTTP request");
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri.Should().Be(
            new Uri($"http://test-zilean.local{config.Ingestion.ZileanEndpointSuffix}"),
            "because Zilean endpoints must append the ZileanEndpointSuffix");
        handler.LastRequest.Headers.GetValues("X-Api-Key").Should().ContainSingle().Which.Should().Be(
            "secret-key-123",
            "because Zilean endpoints authenticate via the X-Api-Key header set from endpoint.ApiKey");
    }

    [Fact]
    public async Task Generic_AppendsEndpointSuffix_AddsAuthorizationWhenNonEmpty()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "[]");
        var config = DefaultConfig();
        var processor = CreateProcessor(handler, config);

        var endpoint = new GenericEndpoint
        {
            Url = "http://test-generic.local",
            EndpointType = GenericEndpointType.Generic,
            EndpointSuffix = "/custom/path",
            Authorization = "Bearer token-abc",
        };

        await processor.ProcessEndpointAsync(endpoint, CancellationToken.None);

        handler.CallCount.Should().Be(1, "because the producer should issue exactly one HTTP request");
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri.Should().Be(
            new Uri("http://test-generic.local/custom/path"),
            "because Generic endpoints append the per-endpoint EndpointSuffix");
        handler.LastRequest.Headers.GetValues("Authorization").Should().ContainSingle().Which.Should().Be(
            "Bearer token-abc",
            "because Generic endpoints with a non-empty Authorization send it as a header");
    }

    [Fact]
    public async Task Generic_WithEmptyAuthorization_OmitsAuthorizationHeader()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "[]");
        var config = DefaultConfig();
        var processor = CreateProcessor(handler, config);

        var endpoint = new GenericEndpoint
        {
            Url = "http://test-generic2.local",
            EndpointType = GenericEndpointType.Generic,
            EndpointSuffix = "/api/torrents",
            Authorization = "",
        };

        await processor.ProcessEndpointAsync(endpoint, CancellationToken.None);

        handler.CallCount.Should().Be(1, "because the producer should issue exactly one HTTP request");
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri.Should().Be(
            new Uri("http://test-generic2.local/api/torrents"),
            "because Generic endpoints append the per-endpoint EndpointSuffix");
        handler.LastRequest.Headers.Contains("Authorization").Should().BeFalse(
            "because an empty Authorization string must not produce an Authorization header");
    }

    [Fact]
    public async Task UnknownEndpointType_ThrowsInvalidOperationException_SwallowedByCatch()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "[]");
        var config = DefaultConfig();
        var processor = CreateProcessor(handler, config);

        var endpoint = new GenericEndpoint
        {
            Url = "http://test-unknown.local",
            // An out-of-range enum value exercises the default switch arm.
            EndpointType = (GenericEndpointType)99,
        };

        // The exception is caught inside ProduceEntriesAsync's catch-all; no exception escapes.
        var act = async () => await processor.ProcessEndpointAsync(endpoint, CancellationToken.None);

        await act.Should().NotThrowAsync(
            "because the unknown-endpoint InvalidOperationException is swallowed by the producer catch-all");
        handler.CallCount.Should().Be(0,
            "because the switch throws before any HTTP request is issued");
    }

    [Fact]
    public async Task Http500_SwallowsException_CompletesWithoutThrowing()
    {
        var handler = new RecordingHandler(HttpStatusCode.InternalServerError, "internal error");
        var config = DefaultConfig();
        var processor = CreateProcessor(handler, config);

        var endpoint = new GenericEndpoint
        {
            Url = "http://test-500.local",
            EndpointType = GenericEndpointType.Zurg,
        };

        // EnsureSuccessStatusCode throws → caught by the producer catch-all → returns normally.
        var act = async () => await processor.ProcessEndpointAsync(endpoint, CancellationToken.None);

        await act.Should().NotThrowAsync(
            "because the HTTP 500 EnsureSuccessStatusCode exception is swallowed by the producer catch-all");
        handler.CallCount.Should().Be(1,
            "because the request was issued before the status check failed");
    }
}