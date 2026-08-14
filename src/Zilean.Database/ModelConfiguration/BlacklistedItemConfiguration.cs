namespace Zilean.Database.ModelConfiguration;

/// <summary>
/// Configures the <see cref="BlacklistedItem"/> entity — table name, primary key (<c>InfoHash</c>), column types, JSON property names, default timestamp, and a unique index on <c>InfoHash</c>.
/// </summary>
public class BlacklistedItemConfiguration: IEntityTypeConfiguration<BlacklistedItem>
{
    /// <summary>
    /// Applies the entity configuration to <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The entity type builder for <see cref="BlacklistedItem"/>.</param>
    public void Configure(EntityTypeBuilder<BlacklistedItem> builder)
    {
        builder.ToTable("BlacklistedItems");

        builder.HasKey(i => i.InfoHash);

        builder.Property(i => i.InfoHash)
            .HasColumnType("text")
            .HasAnnotation("Relational:JsonPropertyName", "info_hash");

        builder.Property(i => i.Reason)
            .IsRequired()
            .HasColumnType("text")
            .HasAnnotation("Relational:JsonPropertyName", "reason");

        builder.Property(t => t.BlacklistedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now() at time zone 'utc'")
            .HasAnnotation("Relational:JsonPropertyName", "blacklisted_at");

        builder.HasIndex(i => i.InfoHash)
            .IsUnique();
    }
}