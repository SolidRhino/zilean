using Zilean.ApiService.Features.Torznab;
using Zilean.Shared.Features.Torznab;
using Zilean.Shared.Features.Torznab.Parameters;
using Zilean.Tests.Collections;

namespace Zilean.Tests.Tests;

/// <summary>
/// Pure unit tests for TorznabCapabilities-driven validation (CanHandleQuery and
/// ValidateQueryAgainstCapabilities). These mutate the static *SearchParams lists
/// (snapshot/restore in finally) to simulate a disabled capability, then assert the
/// throw→error-900 and 201 branches are reachable. No host, no DB.
/// </summary>
[Collection(nameof(ApiTestCollection))]
public class TorznabQueryValidationTests
{
    private static TorznabQuery MovieImdbQuery() => new()
    {
        QueryType = "movie",
        ImdbID = "tt0133093",
        SearchTerm = "The Matrix",
    };

    private static ILogger DummyLogger() => Substitute.For<ILogger>();

    [Fact]
    public void ValidateQueryAgainstCapabilities_ThrowsWhenImdbCapabilityOff()
    {
        var saved = TorznabCapabilities.MovieSearchParams.ToList();
        try
        {
            // Remove ImdbId so MovieSearchImdbAvailable == false.
            TorznabCapabilities.MovieSearchParams.Remove(MovieSearch.ImdbId);

            var query = MovieImdbQuery();

            var act = () => query.ValidateQueryAgainstCapabilities(DummyLogger());

            act.Should().Throw<NotSupportedException>(
                "because an IMDb movie search with MovieSearchImdbAvailable disabled must throw");
        }
        finally
        {
            TorznabCapabilities.MovieSearchParams.Clear();
            TorznabCapabilities.MovieSearchParams.AddRange(saved);
        }
    }

    [Fact]
    public void CanHandleQuery_ReturnsFalse_WhenImdbCapabilityOff()
    {
        var saved = TorznabCapabilities.MovieSearchParams.ToList();
        try
        {
            // Clear entirely → MovieSearchImdbAvailable == false.
            TorznabCapabilities.MovieSearchParams.Clear();

            var query = MovieImdbQuery();

            query.CanHandleQuery().Should().BeFalse(
                "because a movie IMDb search is not handleable when MovieSearchImdbAvailable is false; "
                + "this drives the error-201 branch in ValidateAndPrepareQuery");
        }
        finally
        {
            TorznabCapabilities.MovieSearchParams.Clear();
            TorznabCapabilities.MovieSearchParams.AddRange(saved);
        }
    }

    [Fact]
    public void CanHandleQuery_ReturnsTrue_WhenAllCapabilitiesOn()
    {
        var query = MovieImdbQuery();
        query.CanHandleQuery().Should().BeTrue(
            "because the default MovieSearchParams includes ImdbId");
    }

    [Fact]
    public void ValidateQueryAgainstCapabilities_PassesWhenAllCapabilitiesOn()
    {
        var query = MovieImdbQuery();

        var act = () => query.ValidateQueryAgainstCapabilities(DummyLogger());

        act.Should().NotThrow(
            "because the default capabilities support an IMDb movie search");
    }
}