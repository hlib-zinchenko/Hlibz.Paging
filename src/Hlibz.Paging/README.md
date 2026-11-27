![Hlibz.Paging icon](https://raw.githubusercontent.com/hlib-zinchenko/Hlibz.Paging/main/docs/icon.png)

# Hlibz.Paging

[![NuGet](https://img.shields.io/nuget/v/Hlibz.Paging.svg)](https://www.nuget.org/packages/Hlibz.Paging)
[![CI](https://github.com/hlib-zinchenko/Hlibz.Paging/actions/workflows/ci.yml/badge.svg)](https://github.com/hlib-zinchenko/Hlibz.Paging/actions/workflows/ci.yml)

Paging and sorting for list endpoints: the DTO says what clients may sort by, a client's sort
string is checked against it, and the LINQ that sorts and pages is built for you. No
dependencies.

```bash
dotnet add package Hlibz.Paging
```

```csharp
using System.ComponentModel;
using Hlibz.Paging;

public sealed class OrderDto
{
    [Sortable]
    public int Id { get; init; }

    [Sortable]
    public string CustomerName { get; init; } = "";

    [Sortable(IsDefault = true, DefaultDirection = ListSortDirection.Descending)]
    public DateTime CreatedAt { get; init; }
}

// sort = "customer_name,created_at:desc" from the query string
if (!SortableDto.For<OrderDto>().TryParse(sort, out SortingParams? sorting, out string? error))
{
    return Results.ValidationProblem(new Dictionary<string, string[]> { ["sort"] = [error] });
}

IOrderedQueryable<OrderDto> sorted = orderDtos.OrderBy(sorting);                    // any IQueryable
PagedData<OrderDto> page = list.Paginate(sorting, new PagingParams(page, perPage));  // in memory

SortableDto.ValidateAll([typeof(OrderDto).Assembly]);   // at startup
```

- `[Sortable]`: which DTO properties clients may sort by, and the default order.
- `SortableDto`: parses a sort string (`key[:asc|desc],...`, keys in any casing) against the
  DTO, accepting only its sortable properties; errors name what can be sorted by.
  A DTO without exactly one default is build error `HPAG001`; `ValidateAll` checks again at
  startup: every DTO in the assemblies you pass (listed by the package's source generator, no
  scanning; abstract bases skipped), or just the DTO types you pass.
- `[SortableBy(typeof(OrderDto))]`: the same check as a DataAnnotations rule on a query's
  sort string, so whatever validates the query rejects a bad sort; `SortableDto.Parse` then turns
  the checked string into sorting.
- `PagingParams`: page from 1 (default 1) and page size (default 100); missing or out-of-range
  values fall back to the defaults. `PagingParams.All` is one page with every item.
- `SortingParams`: sort keys in order. `OrderBy(SortingParams)` sorts strings
  case-insensitively, then by `Id` to break ties; with no keys, by `Id`.
- `PagedData<T>`: the page's items and `PagingMetadata` (`TotalItems`, `CurrentPage`, `PerPage`,
  `TotalPages`, `HasNextPage`). Every property is `required`, so an OpenAPI schema lists them all
  as required, and both read back from JSON.

[Hlibz.Paging.EntityFrameworkCore](https://www.nuget.org/packages/Hlibz.Paging.EntityFrameworkCore)'s
`PaginateAsync` loads a page from the database, and
[Hlibz.OpenApi.Paging](https://www.nuget.org/packages/Hlibz.OpenApi.Paging) describes a
`[SortableBy]` parameter or property in OpenAPI documents. With
[Hlibz.Results](https://www.nuget.org/packages/Hlibz.Results), return a bad sort string as
`new ValidationFailed(new Dictionary<string, string[]> { ["sort"] = [error] })`, so it gets the
same error body as every other failure.

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
