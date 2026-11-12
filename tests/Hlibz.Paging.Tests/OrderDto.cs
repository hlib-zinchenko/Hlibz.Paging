using System.ComponentModel;

namespace Hlibz.Paging.Tests;

public sealed class OrderDto
{
    [Sortable]
    public int Id { get; init; }

    [Sortable]
    public string CustomerName { get; init; } = string.Empty;

    [Sortable(IsDefault = true, DefaultDirection = ListSortDirection.Descending)]
    public DateTime CreatedAt { get; init; }

    public decimal Total { get; init; }

    public string? Note { get; init; }
}

public sealed class NoDefaultDto
{
    [Sortable]
    public int Id { get; init; }
}

public sealed class TwoDefaultsDto
{
    [Sortable(IsDefault = true)]
    public int Id { get; init; }

    [Sortable(IsDefault = true)]
    public string Name { get; init; } = string.Empty;
}

public sealed class OpenRow<T>
{
    [Sortable]
    public T? Value { get; init; }
}
