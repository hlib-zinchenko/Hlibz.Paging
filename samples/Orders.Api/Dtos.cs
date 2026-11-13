using System.ComponentModel;

using Hlibz.Paging;

namespace Orders.Api;

/// <summary>An order as clients see it: three sortable properties, newest first by default.</summary>
internal sealed class OrderDto
{
    [Sortable]
    public int Id { get; init; }

    [Sortable]
    public required string CustomerName { get; init; }

    [Sortable(IsDefault = true, DefaultDirection = ListSortDirection.Descending)]
    public DateTime CreatedAt { get; init; }

    /// <summary>Not sortable: <c>sort=total</c> is a 400 naming what is.</summary>
    public decimal Total { get; init; }
}

/// <summary>A customer, sorted by name by default.</summary>
internal sealed record CustomerDto(
    [property: Sortable] int Id,
    [property: Sortable(IsDefault = true)] string Name,
    [property: Sortable] string Country);

/// <summary>
/// Customers held in memory, as a list from a cache or another service would be: paged with
/// <c>Paginate</c>, no database involved. The database is seeded from the same list.
/// </summary>
internal static class CustomerDirectory
{
    public static IReadOnlyList<CustomerDto> All { get; } =
    [
        new(1, "Olena Kovalenko", "UA"),
        new(2, "anna schmidt", "DE"),
        new(3, "Kenji Watanabe", "JP"),
        new(4, "Bruno Costa", "BR"),
        new(5, "Andriy Melnyk", "UA"),
        new(6, "Marie Dubois", "FR"),
        new(7, "Lukas Novak", "CZ"),
    ];
}
