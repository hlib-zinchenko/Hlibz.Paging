using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Hlibz.Paging.Internal;

/// <summary>
/// What a <see cref="SortingParams"/> sorts <c>T</c> by: each sort key's property in turn, then
/// <c>Id</c> when <c>T</c> has one and no key names it, so items with equal keys keep one order.
/// With no sort keys, <c>Id</c> alone. Shared by the query sort (expression trees) and the
/// in-memory one (delegates), so both order alike.
/// </summary>
internal static class SortPlan
{
    private const string IdProperty = "Id";

    /// <summary>
    /// Gets the properties to sort <typeparamref name="T"/> by, in order, with their directions.
    /// </summary>
    /// <typeparam name="T">The item type the sort keys name properties of.</typeparam>
    /// <param name="sortingParams">The sorting.</param>
    /// <returns>At least one property.</returns>
    /// <exception cref="SortingParamNotFoundException">
    /// A sort key (or, with none, <c>Id</c>) names no public property of <typeparamref name="T"/>.
    /// </exception>
    public static List<(PropertyInfo Property, ListSortDirection Direction)> For<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
        SortingParams sortingParams)
    {
        IReadOnlyList<SortingParam> keys = sortingParams.Params.Count > 0
            ? sortingParams.Params
            : [SortingParam.Ascending(IdProperty)];
        List<(PropertyInfo Property, ListSortDirection Direction)> sortBy =
        [
            .. keys.Select((key, index) => (PropertyLookup.Find(typeof(T), keys, index), key.SortDirection)),
        ];
        if (PropertyLookup.FindOrDefault(typeof(T), IdProperty) is { } id
            && !sortBy.Exists(pair => pair.Property == id))
        {
            sortBy.Add((id, ListSortDirection.Ascending));
        }

        return sortBy;
    }

    /// <summary>
    /// Sorts <paramref name="source"/> in memory by <paramref name="sortBy"/>: a string by its
    /// upper case (current culture), <see langword="null"/> first, as the query sort does in
    /// memory.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The items.</param>
    /// <param name="sortBy">The plan, from <see cref="For{T}"/>.</param>
    /// <returns>The sorted items.</returns>
    public static IOrderedEnumerable<T> Sort<T>(
        IEnumerable<T> source,
        List<(PropertyInfo Property, ListSortDirection Direction)> sortBy)
    {
        IOrderedEnumerable<T>? sorted = null;
        foreach ((PropertyInfo property, ListSortDirection direction) in sortBy)
        {
            Func<T, object?> key = KeyOf<T>(property);
            bool descending = direction == ListSortDirection.Descending;
            sorted = (sorted, descending) switch
            {
                (null, false) => source.OrderBy(key, SortKeyComparer.Instance),
                (null, true) => source.OrderByDescending(key, SortKeyComparer.Instance),
                (_, false) => sorted.ThenBy(key, SortKeyComparer.Instance),
                (_, true) => sorted.ThenByDescending(key, SortKeyComparer.Instance),
            };
        }

        return sorted ?? throw new ArgumentException("A sort plan has at least one property.", nameof(sortBy));
    }

    private static Func<T, object?> KeyOf<T>(PropertyInfo property) =>
        property.PropertyType == typeof(string)
            ? item => ((string?)property.GetValue(item))?.ToUpper(CultureInfo.CurrentCulture)
            : item => property.GetValue(item);

    /// <summary>
    /// Compares boxed sort keys as <see cref="Comparer{T}.Default"/> compares the unboxed ones:
    /// <see langword="null"/> first, strings by the current culture.
    /// </summary>
    private sealed class SortKeyComparer : IComparer<object?>
    {
        public static readonly SortKeyComparer Instance = new();

        public int Compare(object? x, object? y) =>
            x is string left && y is string right
                ? Comparer<string>.Default.Compare(left, right)
                : Comparer<object?>.Default.Compare(x, y);
    }
}
