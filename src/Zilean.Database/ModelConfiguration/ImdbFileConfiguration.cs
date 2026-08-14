namespace Zilean.Database.ModelConfiguration;

/// <summary>
/// Configures the <see cref="ImdbFile"/> entity — table name, primary key (<c>ImdbId</c>), column types, and a unique index on <c>ImdbId</c>.
/// </summary>
public class ImdbFileConfiguration:  IEntityTypeConfiguration<ImdbFile>
{
    /// <summary>
    /// Applies the entity configuration to <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The entity type builder for <see cref="ImdbFile"/>.</param>
    public void Configure(EntityTypeBuilder<ImdbFile> builder)
    {
        builder.ToTable("ImdbFiles");

        builder.HasKey(i => i.ImdbId);

        builder.Property(i => i.ImdbId)
            .HasColumnType("text");

        builder.Property(i => i.Category)
            .HasColumnType("text");

        builder.Property(i => i.Title)
            .HasColumnType("text");

        builder.Property(i => i.Adult)
            .HasColumnType("boolean");

        builder.Property(i => i.Year)
            .HasColumnType("integer");

        builder.HasIndex(i => i.ImdbId)
            .IsUnique();
    }
}