![Hlibz.Paging.EntityFrameworkCore icon](https://raw.githubusercontent.com/hlib-zinchenko/Hlibz.Paging/main/docs/icon.png)

# Hlibz.Paging.EntityFrameworkCore

[![NuGet](https://img.shields.io/nuget/v/Hlibz.Paging.EntityFrameworkCore.svg)](https://www.nuget.org/packages/Hlibz.Paging.EntityFrameworkCore)
[![CI](https://github.com/hlib-zinchenko/Hlibz.Paging/actions/workflows/ci.yml/badge.svg)](https://github.com/hlib-zinchenko/Hlibz.Paging/actions/workflows/ci.yml)

`PaginateAsync` for Entity Framework Core, built on
[Hlibz.Paging](https://www.nuget.org/packages/Hlibz.Paging).

```bash
dotnet add package Hlibz.Paging.EntityFrameworkCore
```

```csharp
using Hlibz.Paging.EntityFrameworkCore;

PagedData<OrderDto> page = await db.Orders
    .Where(order => order.CustomerId == customerId)
    .Select(order => new OrderDto { Id = order.Id, CreatedAt = order.CreatedAt })
    .PaginateAsync(sortingParams, pagingParams, cancellationToken);
```

It sorts in the database (strings case-insensitively), runs a `COUNT`, then loads the page with
`OFFSET`/`LIMIT`; an empty result skips the second query, and `PagingParams.All` loads everything
in one. Sort the projected DTO when its property names differ from the entity's: sort keys name
the type being sorted.

## Related packages

- [Hlibz.Paging](https://www.nuget.org/packages/Hlibz.Paging): `[Sortable]`, `SortableDto` and
  `PagingParams`, which this package builds on.

Full documentation and a sample: [github.com/hlib-zinchenko/Hlibz.Paging](https://github.com/hlib-zinchenko/Hlibz.Paging).
