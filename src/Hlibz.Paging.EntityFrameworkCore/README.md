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

Targets .NET 10 and 11.

## A family of packages

One of five packages I build to set up new .NET APIs the way I want them: Hlibz.Results,
Hlibz.UseCases, Hlibz.MinimalApi.Endpoints, Hlibz.OpenApi and Hlibz.Paging. Each works on its own,
and they work best together, as in [Hlibz.Stack](https://github.com/hlib-zinchenko/Hlibz.Stack). New
minor and major versions come often, a new major whenever .NET gives me a better way to do
something. Breaking changes only ever come in a major and are listed first in its
[release notes](https://github.com/hlib-zinchenko/Hlibz.Paging/releases). You're welcome to use the
packages, and to [open an issue](https://github.com/hlib-zinchenko/Hlibz.Paging/issues) when
something is wrong or missing.

Full documentation and samples: [github.com/hlib-zinchenko/Hlibz.Paging](https://github.com/hlib-zinchenko/Hlibz.Paging).
