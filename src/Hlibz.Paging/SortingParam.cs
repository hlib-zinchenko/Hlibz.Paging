using System.ComponentModel;

namespace Hlibz.Paging;

/// <summary>
/// One sort key: a property and a direction.
/// </summary>
public sealed class SortingParam
{
    private SortingParam(ListSortDirection sortDirection, string sortBy)
    {
        SortDirection = sortDirection;
        SortBy = sortBy;
    }

    /// <summary>
    /// Gets the direction.
    /// </summary>
    public ListSortDirection SortDirection { get; }

    /// <summary>
    /// Gets the property to sort by: its name as declared or in any casing (<c>CreatedAt</c>,
    /// <c>createdAt</c>, <c>created_at</c>, <c>created-at</c>).
    /// </summary>
    public string SortBy { get; }

    /// <summary>
    /// Sorts by <paramref name="property"/>, smallest first.
    /// </summary>
    /// <param name="property">The property to sort by.</param>
    /// <returns>The sort key.</returns>
    public static SortingParam Ascending(string property)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(property);
        return new SortingParam(ListSortDirection.Ascending, property);
    }

    /// <summary>
    /// Sorts by <paramref name="property"/>, largest first.
    /// </summary>
    /// <param name="property">The property to sort by.</param>
    /// <returns>The sort key.</returns>
    public static SortingParam Descending(string property)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(property);
        return new SortingParam(ListSortDirection.Descending, property);
    }

    /// <summary>
    /// Sorts by <paramref name="property"/> in <paramref name="direction"/>.
    /// </summary>
    /// <param name="property">The property to sort by.</param>
    /// <param name="direction">The direction.</param>
    /// <returns>The sort key.</returns>
    public static SortingParam Create(string property, ListSortDirection direction) =>
        direction == ListSortDirection.Descending ? Descending(property) : Ascending(property);
}
