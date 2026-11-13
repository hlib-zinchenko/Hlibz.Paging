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

* `[Sortable]`: which DTO properties clients may sort by, and the default order.
* `SortableDto`: parses a sort string (`key[:asc|desc],...`, keys in any casing) against the
  DTO, accepting only its sortable properties; errors name what can be sorted by.
  `ValidateAll` fails startup on a DTO without exactly one default: every DTO in the assemblies
  you pass (abstract bases skipped), or just the DTO types you pass.
* `PagingParams`: page from 1 (default 1) and page size (default 100); missing or out-of-range
  values fall back to the defaults. `PagingParams.All` is one page with every item.
* `SortingParams`: sort keys in order. `OrderBy(SortingParams)` sorts strings
  case-insensitively; with no keys, by `Id`.
* `PagedData<T>`: the page's items and `PagingMetadata` (`TotalItems`, `CurrentPage`, `PerPage`,
  `TotalPages`, `HasNextPage`). Every property is `required`, so an OpenAPI schema lists them all
  as required, and both read back from JSON.

## Related packages

- [Hlibz.Paging.EntityFrameworkCore](https://www.nuget.org/packages/Hlibz.Paging.EntityFrameworkCore):
  `PaginateAsync` loads a page from the database.
- [Hlibz.Results](https://www.nuget.org/packages/Hlibz.Results): return a bad sort string as
  `Result.Invalid(new Dictionary<string, string[]> { ["sort"] = [error] })`, so it gets the same
  error body as every other failure.

Full documentation and a sample: [github.com/hlib-zinchenko/Hlibz.Paging](https://github.com/hlib-zinchenko/Hlibz.Paging).
