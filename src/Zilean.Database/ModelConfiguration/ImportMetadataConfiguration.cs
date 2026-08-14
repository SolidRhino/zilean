namespace Zilean.Database.ModelConfiguration;

/// <summary>
/// Configures the <see cref="ImportMetadata"/> entity — table name, primary key (<c>Key</c>), and JSONB <c>Value</c> column.
/// </summary>
public class ImportMetadataConfiguration : IEntityTypeConfiguration<ImportMetadata>
{
    /// <summary>
    /// Applies the entity configuration to <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The entity type builder for <see cref="ImportMetadata"/>.</param>
    public void Configure(EntityTypeBuilder<ImportMetadata> builder)
    {
        builder.ToTable("ImportMetadata");

        builder.HasKey(x => x.Key);

        builder.Property(e => e.Value)
            .IsRequired()
            .HasColumnType("jsonb");
    }
}