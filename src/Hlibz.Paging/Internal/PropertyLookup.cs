using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Hlibz.Paging.Internal;

/// <summary>
/// Matches sort keys to properties whatever the casing: <c>CreatedAt</c>, <c>createdAt</c>,
/// <c>created_at</c> and <c>created-at</c> all name the same property. Each type's properties are
/// read once.
/// </summary>
internal static class PropertyLookup
{
    /// <summary>
    /// Each type's public instance properties by <see cref="Normalize">normalized</see> name; the
    /// first of two that normalize alike wins, in <see cref="Type.GetProperties()"/> order.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> Properties = new();

    /// <summary>
    /// The property the sort key at <paramref name="index"/> names.
    /// </summary>
    /// <param name="type">The sorted type.</param>
    /// <param name="keys">Every sort key, so the exception can name all that name nothing.</param>
    /// <param name="index">The sort key to look up.</param>
    /// <returns>The public instance property.</returns>
    /// <exception cref="SortingParamNotFoundException">It names no property.</exception>
    public static PropertyInfo Find(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type,
        IReadOnlyList<SortingParam> keys,
        int index)
    {
        Dictionary<string, PropertyInfo> properties = ByName(type);
        if (properties.GetValueOrDefault(Normalize(keys[index].SortBy)) is { } property)
        {
            return property;
        }

        throw new SortingParamNotFoundException(
            type,
            [.. keys.Select(key => key.SortBy).Where(sortBy => !properties.ContainsKey(Normalize(sortBy)))]);
    }

    /// <summary>
    /// The property <paramref name="sortBy"/> names, if any.
    /// </summary>
    /// <param name="type">The sorted type.</param>
    /// <param name="sortBy">A sort key, in any casing.</param>
    /// <returns>The public instance property, or <see langword="null"/>.</returns>
    public static PropertyInfo? FindOrDefault(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type,
        string sortBy) =>
        ByName(type).GetValueOrDefault(Normalize(sortBy));

    /// <summary>
    /// A name with its <c>_</c> and <c>-</c> removed; compare the results ignoring case.
    /// </summary>
    /// <param name="name">A sort key or a property name.</param>
    /// <returns>The name to compare.</returns>
    public static string Normalize(string name) =>
        name.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

    private static Dictionary<string, PropertyInfo> ByName(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        if (Properties.TryGetValue(type, out Dictionary<string, PropertyInfo>? cached))
        {
            return cached;
        }

        Dictionary<string, PropertyInfo> properties = new(StringComparer.OrdinalIgnoreCase);
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            properties.TryAdd(Normalize(property.Name), property);
        }

        return Properties.GetOrAdd(type, properties);
    }
}
