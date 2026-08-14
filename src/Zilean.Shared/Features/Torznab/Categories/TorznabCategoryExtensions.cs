namespace Zilean.Shared.Features.Torznab.Categories;

/// <summary>
/// Provides extension methods for <see cref="TorznabCategory"/> collections.
/// </summary>
public static class TorznabCategoryExtensions
{
    /// <summary>
    /// Builds a sorted category tree with subcategories ordered by ID and top-level
    /// categories sorted by ID (non-numeric IDs sorted last).
    /// </summary>
    /// <param name="categories">The source categories to build the tree from.</param>
    /// <returns>A new sorted list of categories with sorted subcategories.</returns>
    public static List<TorznabCategory> GetTorznabCategoryTree(this List<TorznabCategory> categories)
    {
        var sortedTree = categories
            .Select(c =>
        {
            var sortedSubCats = c.SubCategories.OrderBy(x => x.Id);
            var newCat = new TorznabCategory(c.Id, c.Name);
            newCat.SubCategories.AddRange(sortedSubCats);
            return newCat;
        }).OrderBy(x => x.Id >= 100000 ? "zzz" + x.Name : x.Id.ToString()).ToList();

        return sortedTree;
    }
}
