using Zilean.ApiService.Features.Dashboard.Components.Pages.Dashboard;
using Zilean.Database.Services;
using Zilean.Shared.Features.Blacklist;

namespace Zilean.Tests.Tests;

/// <summary>
/// Integration tests for <see cref="DashboardBlacklistDataAdapter.InsertAsync"/>, which is
/// called by the Syncfusion grid's Add dialog. Verifies that a <see cref="BlacklistItemDetails"/>
/// supplied by the dialog is persisted via <see cref="IBlacklistService.AddAsync"/>, preserving
/// the domain contract (validation, duplicate rejection, torrent removal).
/// </summary>
[Collection(nameof(ApiTestCollection))]
public class DashboardBlacklistDataAdapterTests(PostgresLifecycleFixture fixture)
{
    private static ILogger<DashboardBlacklistDataAdapter> CreateLogger() =>
        NSubstitute.Substitute.For<ILogger<DashboardBlacklistDataAdapter>>();

    private IBlacklistService CreateBlacklistService() =>
        fixture.Factory.Services.GetRequiredService<IBlacklistService>();

    private DashboardBlacklistDataAdapter CreateAdapter() =>
        new(fixture.DbContextFactory, CreateBlacklistService(), CreateLogger());

    [Fact]
    public async Task InsertAsync_WithBlacklistItemDetails_PersistsRecordAndRemovesTorrent()
    {
        // Insert a temp torrent so we can verify the domain contract removes it on blacklist.
        var hash = $"adapter-insert-{Guid.NewGuid():N}"[..39];
        await using var seedContext = await fixture.DbContextFactory.CreateDbContextAsync();
        seedContext.Torrents.Add(new TorrentInfo
        {
            InfoHash = hash,
            RawTitle = "Adapter.Test.Torrent.1080p",
            ParsedTitle = "Adapter Test Torrent",
            NormalizedTitle = "adapter test torrent",
            CleanedParsedTitle = "adapter test torrent",
            Category = "movie",
            Year = 2024,
            Resolution = "1080p",
            Size = "1.0 GB",
            Seasons = [],
            Episodes = [],
            Languages = ["English"],
            IngestedAt = DateTime.UtcNow,
        });
        await seedContext.SaveChangesAsync();

        var adapter = CreateAdapter();
        var incoming = new BlacklistItemDetails
        {
            InfoHash = hash,
            Reason = "adapter test reason",
        };

        BlacklistedItem? persisted = null;
        try
        {
            var result = await adapter.InsertAsync(null!, incoming, "InfoHash");

            result.Should().BeSameAs(incoming,
                "because InsertAsync returns the supplied value on success");

            await using var dbContext = await fixture.DbContextFactory.CreateDbContextAsync();
            persisted = await dbContext.BlacklistedItems
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.InfoHash == hash);

            persisted.Should().NotBeNull(
                "because InsertAsync must persist the BlacklistedItem record via IBlacklistService.AddAsync");
            persisted!.Reason.Should().Be("adapter test reason",
                "because the reason from the dialog must be stored verbatim");
            persisted.BlacklistedAt.Should().NotBeNull(
                "because IBlacklistService.AddAsync sets BlacklistedAt server-side to DateTime.UtcNow");

            // The domain contract removes the matching torrent from the Torrents table.
            var torrentExists = await dbContext.Torrents.AnyAsync(x => x.InfoHash == hash);
            torrentExists.Should().BeFalse(
                "because IBlacklistService.AddAsync removes the matching torrent when blacklisting");
        }
        finally
        {
            if (persisted != null)
            {
                await using var cleanupContext = await fixture.DbContextFactory.CreateDbContextAsync();
                cleanupContext.BlacklistedItems.Remove(persisted);
                await cleanupContext.SaveChangesAsync();
            }
        }
    }

    [Fact]
    public async Task InsertAsync_WithNonMatchingValue_ReturnsNull()
    {
        var adapter = CreateAdapter();

        var result = await adapter.InsertAsync(null!, "not-a-blacklist-item", "InfoHash");

        result.Should().BeNull(
            "because InsertAsync returns null when the value is not a BlacklistItemDetails");
    }

    [Fact]
    public async Task InsertAsync_WithEmptyInfoHash_ReturnsNull_DomainContractRejects()
    {
        // The Add dialog can submit an empty InfoHash if the user clears the field.
        // The domain contract (IBlacklistService.AddAsync) rejects empty hashes with
        // BlacklistResult.InvalidHash, so InsertAsync returns null.
        var adapter = CreateAdapter();
        var incoming = new BlacklistItemDetails
        {
            InfoHash = "",
            Reason = "empty hash test",
        };

        var result = await adapter.InsertAsync(null!, incoming, "InfoHash");

        result.Should().BeNull(
            "because IBlacklistService.AddAsync rejects an empty InfoHash with InvalidHash");

        // Verify no record was persisted.
        await using var dbContext = await fixture.DbContextFactory.CreateDbContextAsync();
        var persisted = await dbContext.BlacklistedItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.InfoHash == "" && x.Reason == "empty hash test");
        persisted.Should().BeNull(
            "because the domain contract must not persist an invalid blacklist entry");
    }

    [Fact]
    public async Task InsertAsync_WithDuplicateHash_ReturnsNull_DomainContractRejects()
    {
        var hash = $"adapter-dup-{Guid.NewGuid():N}"[..39];
        var adapter = CreateAdapter();

        // First insert succeeds.
        var first = new BlacklistItemDetails { InfoHash = hash, Reason = "first insert" };
        var firstResult = await adapter.InsertAsync(null!, first, "InfoHash");
        firstResult.Should().BeSameAs(first, "because the first insert must succeed");

        // Second insert with same hash is rejected by the domain contract.
        var second = new BlacklistItemDetails { InfoHash = hash, Reason = "duplicate" };
        var secondResult = await adapter.InsertAsync(null!, second, "InfoHash");
        secondResult.Should().BeNull(
            "because IBlacklistService.AddAsync rejects a duplicate hash with AlreadyBlacklisted");

        // Cleanup.
        await using var cleanupContext = await fixture.DbContextFactory.CreateDbContextAsync();
        var item = await cleanupContext.BlacklistedItems.FirstOrDefaultAsync(x => x.InfoHash == hash);
        if (item != null)
        {
            cleanupContext.BlacklistedItems.Remove(item);
            await cleanupContext.SaveChangesAsync();
        }
    }
}