using System.Net;

namespace Zilean.Tests.Tests;

[Collection(nameof(ApiTestCollection))]
public class BlacklistEndpointsTests(PostgresLifecycleFixture fixture)
{
    private const string TempTorrentHash = "testhash-blacklist-001-temp-torrent";

    /// <summary>
    /// Inserts a temporary torrent into the DB so the blacklist delete side-effect can be
    /// verified without touching the shared seed data other test classes depend on.
    /// </summary>
    private async Task InsertTempTorrentAsync()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ZileanDbContext>();
        var torrent = new TorrentInfo
        {
            InfoHash = TempTorrentHash,
            RawTitle = "Temp.Torrent.For.Blacklist.Test.1080p",
            ParsedTitle = "Temp Torrent Blacklist Test",
            NormalizedTitle = "temp torrent blacklist test",
            CleanedParsedTitle = "temp torrent blacklist test",
            Category = "movie",
            Year = 2024,
            Resolution = "1080p",
            Size = "1.0 GB",
            Seasons = [],
            Episodes = [],
            Languages = ["English"],
            IngestedAt = DateTime.UtcNow,
        };
        dbContext.Torrents.Add(torrent);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task AddBlacklist_WithEmptyInfoHash_Returns400()
    {
        var client = fixture.Factory.CreateAuthenticatedClient();

        var response = await client.PutAsync("/blacklist/add?info_hash=&reason=test", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "because an empty info_hash must be rejected");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("info_hash is required",
            "because the error message must explain the validation failure");
    }

    [Fact]
    public async Task AddBlacklist_WithEmptyReason_Returns400()
    {
        var client = fixture.Factory.CreateAuthenticatedClient();

        var response = await client.PutAsync("/blacklist/add?info_hash=testhash-blacklist-002&reason=", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "because an empty reason must be rejected");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("reason is required",
            "because the error message must explain the validation failure");
    }

    [Fact]
    public async Task AddBlacklist_WithAlreadyBlacklistedHash_Returns409()
    {
        var client = fixture.Factory.CreateAuthenticatedClient();
        var hash = "testhash-blacklist-003";

        var firstAdd = await client.PutAsync($"/blacklist/add?info_hash={hash}&reason=test", null);
        firstAdd.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "because the first add of a new hash must succeed before testing the duplicate");
        var response = await client.PutAsync($"/blacklist/add?info_hash={hash}&reason=test", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "because adding the same hash twice must be rejected as a conflict");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("already blacklisted",
            "because the error message must explain the conflict");
    }

    [Fact]
    public async Task AddBlacklist_WithNewHash_Returns204_AndDeletesTorrent()
    {
        await InsertTempTorrentAsync();
        var client = fixture.Factory.CreateAuthenticatedClient();

        var response = await client.PutAsync(
            $"/blacklist/add?info_hash={TempTorrentHash}&reason=test-delete", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "because a new hash that exists in torrents must be blacklisted and the torrent deleted");

        // Verify the torrent was deleted from the DB via the blacklist side-effect.
        using var scope = fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ZileanDbContext>();
        var exists = await dbContext.Torrents.AnyAsync(x => x.InfoHash == TempTorrentHash);
        exists.Should().BeFalse(
            "because blacklisting a hash must delete the matching torrent from the DB");

        var blacklisted = await dbContext.BlacklistedItems.AnyAsync(x => x.InfoHash == TempTorrentHash);
        blacklisted.Should().BeTrue(
            "because blacklisting a hash must persist a BlacklistedItems record");
    }

    [Fact]
    public async Task AddBlacklist_WithNewHashNotInTorrents_Returns204()
    {
        var client = fixture.Factory.CreateAuthenticatedClient();
        var hash = "testhash-blacklist-004-not-in-torrents";

        var response = await client.PutAsync($"/blacklist/add?info_hash={hash}&reason=test", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "because adding a hash that doesn't exist in torrents still succeeds with 204");
    }

    [Fact]
    public async Task RemoveBlacklist_WithEmptyInfoHash_Returns400()
    {
        var client = fixture.Factory.CreateAuthenticatedClient();

        var response = await client.DeleteAsync("/blacklist/remove?infoHash=");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "because an empty infoHash must be rejected");
    }

    [Fact]
    public async Task RemoveBlacklist_WithNotFound_Returns404()
    {
        var client = fixture.Factory.CreateAuthenticatedClient();

        var response = await client.DeleteAsync("/blacklist/remove?infoHash=testhash-blacklist-005-never-added");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because removing a hash that was never blacklisted must return 404");
    }

    [Fact]
    public async Task RemoveBlacklist_WithExistingHash_Returns204_Then404OnReRemove()
    {
        var client = fixture.Factory.CreateAuthenticatedClient();
        var hash = "testhash-blacklist-006";
        var initialAdd = await client.PutAsync($"/blacklist/add?info_hash={hash}&reason=test", null);
        initialAdd.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "because the initial add must succeed before testing removal");

        var removeResponse = await client.DeleteAsync($"/blacklist/remove?infoHash={hash}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "because removing an existing blacklisted hash must succeed with 204");

        var removeAgainResponse = await client.DeleteAsync($"/blacklist/remove?infoHash={hash}");
        removeAgainResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because removing the same hash again after removal must return 404");
    }
}