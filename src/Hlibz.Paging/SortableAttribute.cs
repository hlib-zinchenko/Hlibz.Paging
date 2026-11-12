using System.ComponentModel;

namespace Hlibz.Paging;

/// <summary>
/// Marks a DTO property clients may sort by. Exactly one sortable property of a DTO is its
/// default order (<see cref="IsDefault"/>).
/// </summary>
/// <example>
/// <code>
/// public sealed record OrderDto(
///     [property: Sortable] int Id,
///     [property: Sortable] string CustomerName,
///     [property: Sortable(IsDefault = true, DefaultDirection = ListSortDirection.Descending)]
///     DateTimeOffset CreatedAt);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SortableAttribute : Attribute
{
    /// <summary>
    /// Gets a value indicating whether a query that names no sort key is sorted by this property.
    /// </summary>
    public bool IsDefault { get; init; }

    /// <summary>
    /// Gets the direction of the default order; only read when <see cref="IsDefault"/> is set.
    /// </summary>
    public ListSortDirection DefaultDirection { get; init; } = ListSortDirection.Ascending;
}
