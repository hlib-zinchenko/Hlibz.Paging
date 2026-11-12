using System.Reflection;

namespace Hlibz.Paging.Internal;

/// <summary>
/// Matches sort keys to properties whatever the casing: <c>CreatedAt</c>, <c>createdAt</c>,
/// <c>created_at</c> and <c>created-at</c> all name the same property.
/// </summary>
internal static class PropertyLookup
{
    /// <summary>
    /// Whether <paramref name="sortBy"/> names <paramref name="propertyName"/>.
    /// </summary>
    /// <param name="sortBy">A sort key, in any casing.</param>
    /// <param name="propertyName">The property's declared name.</param>
    /// <returns><see langword="true"/> when it does.</returns>
    public static bool Matches(string sortBy, string propertyName) =>
        string.Equals(Normalize(sortBy), Normalize(propertyName), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The property the sort key at <paramref name="index"/> names.
    /// </summary>
    /// <param name="type">The sorted type.</param>
    /// <param name="keys">Every sort key, so the exception can name all that name nothing.</param>
    /// <param name="index">The sort key to look up.</param>
    /// <returns>The public instance property.</returns>
    /// <exception cref="SortingParamNotFoundException">It names no property.</exception>
    public static PropertyInfo Find(Type type, IReadOnlyList<SortingParam> keys, int index)
    {
        if (FindOrDefault(type, keys[index].SortBy) is { } property)
        {
            return property;
        }

        throw new SortingParamNotFoundException(
            type,
            [.. keys.Select(key => key.SortBy).Where(sortBy => FindOrDefault(type, sortBy) is null)]);
    }

    private static PropertyInfo? FindOrDefault(Type type, string sortBy) =>
        type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(property => Matches(sortBy, property.Name));

    private static string Normalize(string name) =>
        name.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);
}
