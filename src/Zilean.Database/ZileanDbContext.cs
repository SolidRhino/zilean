namespace Zilean.Database;

/// <summary>
/// EF Core database context for Zilean, exposing entity sets for torrents, IMDb files,
/// parsed pages, import metadata, and blacklisted items.
/// </summary>
public class ZileanDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ZileanDbContext"/> class.
    /// </summary>
    /// <param name="options">The context options configured for Npgsql.</param>
    public ZileanDbContext(DbContextOptions<ZileanDbContext> options): base(options)
    {
    }

    /// <summary>
    /// Configures the entity model by applying all <c>IEntityTypeConfiguration</c> instances.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new TorrentInfoConfiguration());
        modelBuilder.ApplyConfiguration(new ImdbFileConfiguration());
        modelBuilder.ApplyConfiguration(new ParsedPagesConfiguration());
        modelBuilder.ApplyConfiguration(new ImportMetadataConfiguration());
        modelBuilder.ApplyConfiguration(new BlacklistedItemConfiguration());
    }

    /// <summary>
    /// Gets the set of parsed torrent releases (<see cref="TorrentInfo"/>).
    /// </summary>
    public DbSet<TorrentInfo> Torrents => Set<TorrentInfo>();

    /// <summary>
    /// Gets the set of IMDb metadata entries (<see cref="ImdbFile"/>).
    /// </summary>
    public DbSet<ImdbFile> ImdbFiles => Set<ImdbFile>();

    /// <summary>
    /// Gets the set of tracked DMM scrape pages (<see cref="ParsedPages"/>).
    /// </summary>
    public DbSet<ParsedPages> ParsedPages => Set<ParsedPages>();

    /// <summary>
    /// Gets the set of key/value import metadata entries (<see cref="ImportMetadata"/>).
    /// </summary>
    public DbSet<ImportMetadata> ImportMetadata => Set<ImportMetadata>();

    /// <summary>
    /// Gets the set of blacklisted torrent info hashes (<see cref="BlacklistedItem"/>).
    /// </summary>
    public DbSet<BlacklistedItem> BlacklistedItems => Set<BlacklistedItem>();
}