using Zilean.ApiService.Features.Dashboard.Components.Pages.Dashboard;
using Zilean.Shared.Features.Blacklist;

namespace Zilean.Tests.Tests;

/// <summary>
/// Integration tests for <see cref="DashboardBlacklistDataAdapter.InsertAsync"/>, which is
/// called by the Syncfusion grid's Add dialog. Verifies that a <see cref="BlacklistItemDetails"/>
/// supplied by the dialog is persisted as a <see cref="BlacklistedItem"/> with a server-set
/// <c>BlacklistedAt</c> timestamp, and that a non-matching value type returns null.
/// </summary>
[Collection(nameof(ApiTestCollection))]
public class DashboardBlacklistDataAdapterTests(PostgresLifecycleFixture fixture)
{
    private static ILogger<DashboardBlacklistDataAdapter> CreateLogger() =>
        NSubstitute.Substitute.For<ILogger<DashboardBlacklistDataAdapter>>();

    [Fact]
    public async Task InsertAsync_WithBlacklistItemDetails_PersistsRecord()
    {
        var adapter = new DashboardBlacklistDataAdapter(fixture.DbContextFactory, CreateLogger());
        var hash = $"adapter-insert-{Guid.NewGuid():N}"[..39];
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
                "because InsertAsync must persist the BlacklistedItem record");
            persisted!.Reason.Should().Be("adapter test reason",
                "because the reason from the dialog must be stored verbatim");
            persisted.BlacklistedAt.Should().NotBeNull(
                "because InsertAsync sets BlacklistedAt server-side to DateTime.UtcNow");
            persisted.BlacklistedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1),
                "because BlacklistedAt is set to the current UTC time");
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
        var adapter = new DashboardBlacklistDataAdapter(fixture.DbContextFactory, CreateLogger());

        var result = await adapter.InsertAsync(null!, "not-a-blacklist-item", "InfoHash");

        result.Should().BeNull(
            "because InsertAsync returns null when the value is not a BlacklistItemDetails");
    }

    [Fact]
    public async Task InsertAsync_WithEmptyInfoHash_StillPersistsRecord()
    {
        // The Add dialog can submit an empty InfoHash if the user clears the field.
        // The adapter does not validate emptiness (validation is the grid's job);
        // it persists whatever it receives. This test documents that contract.
        var adapter = new DashboardBlacklistDataAdapter(fixture.DbContextFactory, CreateLogger());
        var incoming = new BlacklistItemDetails
        {
            InfoHash = "",
            Reason = "empty hash test",
        };

        BlacklistedItem? persisted = null;
        try
        {
            var result = await adapter.InsertAsync(null!, incoming, "InfoHash");

            result.Should().BeSameAs(incoming,
                "because InsertAsync returns the supplied value even with an empty InfoHash");

            await using var dbContext = await fixture.DbContextFactory.CreateDbContextAsync();
            persisted = await dbContext.BlacklistedItems
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.InfoHash == "" && x.Reason == "empty hash test");
            persisted.Should().NotBeNull(
                "because InsertAsync persists even with an empty InfoHash");
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
}