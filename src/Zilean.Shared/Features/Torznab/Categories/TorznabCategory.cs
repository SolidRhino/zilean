namespace Zilean.Shared.Features.Torznab.Categories;

/// <summary>
/// Represents a Torznab category with an ID, display name, and optional subcategories.
/// </summary>
/// <param name="id">The numeric Torznab category ID.</param>
/// <param name="name">The human-readable category name.</param>
public class TorznabCategory(int id, string name)
{
    /// <summary>
    /// The numeric Torznab category ID.
    /// </summary>
    public int Id { get; } = id;
    /// <summary>
    /// The human-readable category name.
    /// </summary>
    public string Name { get; set; } = name;
    /// <summary>
    /// The subcategories nested under this category.
    /// </summary>
    public List<TorznabCategory> SubCategories { get; private set; } = [];

    /// <summary>
    /// Determines whether this category or any of its subcategories matches the given category.
    /// </summary>
    /// <param name="cat">The category to check for containment.</param>
    /// <returns><c>true</c> if <paramref name="cat"/> equals this category or is a subcategory; otherwise <c>false</c>.</returns>
    public bool Contains(TorznabCategory cat) =>
        Equals(this, cat) || SubCategories.Contains(cat);

    /// <summary>
    /// Serializes the category to a JSON object with <c>ID</c> and <c>Name</c> properties.
    /// </summary>
    /// <returns>A <see cref="JsonObject"/> representing this category.</returns>
    public JsonObject ToJson() =>
        new()
        {
            ["ID"] = Id,
            ["Name"] = Name
        };

    /// <summary>
    /// Determines whether the given object is a <see cref="TorznabCategory"/> with the same ID.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><c>true</c> if <paramref name="obj"/> is a <see cref="TorznabCategory"/>
    /// with the same <see cref="Id"/>.</returns>
    public override bool Equals(object? obj) => (obj as TorznabCategory)?.Id == Id;

    /// <summary>
    /// Returns the hash code based on <see cref="Id"/>.
    /// </summary>
    /// <returns>The hash code of the category ID.</returns>
    public override int GetHashCode() => Id;
    /// <summary>
    /// Creates a copy of this category without its subcategories.
    /// </summary>
    /// <returns>A new <see cref="TorznabCategory"/> with the same ID and name but no subcategories.</returns>
    public TorznabCategory CopyWithoutSubCategories() => new(Id, Name);
}
