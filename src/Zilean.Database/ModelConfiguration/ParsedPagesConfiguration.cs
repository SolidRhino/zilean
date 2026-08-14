namespace Zilean.Database.ModelConfiguration;

/// <summary>
/// Configures the <see cref="ParsedPages"/> entity — table name, primary key (<c>Page</c>), and required properties.
/// </summary>
public class ParsedPagesConfiguration : IEntityTypeConfiguration<ParsedPages>
{
    /// <summary>
    /// Applies the entity configuration to <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The entity type builder for <see cref="ParsedPages"/>.</param>
    public void Configure(EntityTypeBuilder<ParsedPages> builder)
    {
        builder.ToTable("ParsedPages");

        builder.HasKey(x => x.Page);
        builder.Property(x => x.Page).IsRequired();
        builder.Property(x => x.EntryCount).IsRequired();
    }
}