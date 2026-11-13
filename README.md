![Hlibz.Paging icon](https://raw.githubusercontent.com/hlib-zinchenko/Hlibz.Paging/main/docs/icon.png)

# Hlibz.Paging

[![CI](https://github.com/hlib-zinchenko/Hlibz.Paging/actions/workflows/ci.yml/badge.svg)](https://github.com/hlib-zinchenko/Hlibz.Paging/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/hlib-zinchenko/Hlibz.Paging/blob/main/LICENSE)<br>
[![Hlibz.Paging](https://img.shields.io/nuget/v/Hlibz.Paging?label=Hlibz.Paging)](https://www.nuget.org/packages/Hlibz.Paging)
[![downloads](https://img.shields.io/nuget/dt/Hlibz.Paging?label=downloads)](https://www.nuget.org/packages/Hlibz.Paging)<br>
[![Hlibz.Paging.EntityFrameworkCore](https://img.shields.io/nuget/v/Hlibz.Paging.EntityFrameworkCore?label=Hlibz.Paging.EntityFrameworkCore)](https://www.nuget.org/packages/Hlibz.Paging.EntityFrameworkCore)
[![downloads](https://img.shields.io/nuget/dt/Hlibz.Paging.EntityFrameworkCore?label=downloads)](https://www.nuget.org/packages/Hlibz.Paging.EntityFrameworkCore)

Paging and sorting for list endpoints: the DTO says what clients may sort by, a client's sort
string is checked against it, and one call sorts, counts and loads the page.

| Package | What it gives you |
|---|---|
| `Hlibz.Paging` | `PagingParams`, `SortingParams`, `[Sortable]`, `SortableDto` (parses a client's sort string against the DTO, plus a startup check), `PagedData<T>`, `OrderBy(SortingParams)` and in-memory `Paginate`. No dependencies. |
| `Hlibz.Paging.EntityFrameworkCore` | `PaginateAsync`: sort, count and load one page of an `IQueryable`. |

Targets .NET 10 and 11.

## Quick start

Mark what clients may sort by, and the default order:

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

    public decimal Total { get; init; }   // not sortable
}
```

Parse the client's sort string against the DTO, then load the page:

```csharp
using Hlibz.Paging;
using Hlibz.Paging.EntityFrameworkCore;

app.MapGet("/orders", async (
    int? page,
    [FromQuery(Name = "per_page")] int? perPage,
    string? sort,
    ShopDb db,
    CancellationToken ct) =>
{
    if (!SortableDto.For<OrderDto>().TryParse(
            sort, JsonNamingPolicy.SnakeCaseLower, out SortingParams? sorting, out string? error))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["sort"] = [error] });
    }

    PagedData<OrderDto> orders = await db.Orders
        .Select(order => new OrderDto
        {
            Id = order.Id,
            CustomerName = order.Customer.Name,
            CreatedAt = order.CreatedAt,
            Total = order.Total,
        })
        .PaginateAsync(sorting, new PagingParams(page, perPage), ct);
    return Results.Ok(orders);
});
```

`GET /orders?page=2&per_page=20&sort=customer_name,created_at:desc` returns:

```json
{
  "data": [{ "id": 21, "customerName": "Ann", "createdAt": "2026-01-03T00:00:00Z", "total": 12.5 }],
  "pagingMetadata": { "totalItems": 41, "currentPage": 2, "perPage": 20, "totalPages": 3, "hasNextPage": true }
}
```

(property names follow your JSON naming policy).

Check every DTO with a `[Sortable]` property at startup, so a DTO without exactly one default
fails the app rather than the first request that sorts it:

```csharp
SortableDto.ValidateAll([typeof(OrderDto).Assembly]);
```

Abstract types are skipped, so a base class can mark properties `[Sortable]` for the DTOs that
inherit them. When some DTOs have `[Sortable]` properties but are never sorted (say, one that only
inherits them), check just the ones your lists sort:

```csharp
SortableDto.ValidateAll([typeof(OrderDto), typeof(CustomerDto)]);
```

## Sort strings

Comma-separated keys, each a property optionally followed by `:asc` (the default) or `:desc`:
`customer_name,created_at:desc`. An empty sort string is the DTO's default order.

* Keys may be written in any casing: `created_at`, `createdAt`, `CreatedAt` and `created-at` all
  name `CreatedAt`. The parsed keys name the properties as declared.
* Only `[Sortable]` properties are accepted, so clients can't sort by columns you didn't mean to
  expose (or index).
* Errors are written for the client and name what can be sorted by, in the casing you pass
  (`JsonNamingPolicy.SnakeCaseLower` above; as declared without one):

  ```text
  Can't sort by total. Sort by: id, customer_name, created_at.
  ```
* Strings sort case-insensitively (`ToUpper()` in the query).

`SortableDto.For<TDto>()` also exposes `Properties` and `DefaultSorting`, for documenting the
parameter (in OpenAPI, say).

## Paging

`PagingParams(page, perPage)` takes the client's values as they are: a missing or out-of-range
value falls back to page `1` and `100` items. `PagingParams.All` is one page with every item.
`PagedData<T>` holds the page's items and its `PagingMetadata` (`TotalItems`, `CurrentPage`,
`PerPage`, `TotalPages`, `HasNextPage`); `Map` converts the items and keeps the metadata.
Every property of both is `required`, so a JSON schema of a page (the OpenAPI document
Microsoft.AspNetCore.OpenApi serves, say) lists them all as required, and a client in .NET can
read a page back with `JsonSerializer`. Build one with its constructor, which works out
`TotalPages` and `HasNextPage`; the parameterless one is for serializers.

`PaginateAsync` runs a `COUNT`, then loads the page with `OFFSET`/`LIMIT` (no second query when
the count is `0`); `PagingParams.All` loads everything in one query.

## A query class of your own

Binding `page`, `per_page` and `sort` is a few lines, and where your API's conventions live
(parameter names, a maximum page size, how a bad sort becomes a 400), so it isn't in the package:

```csharp
public abstract class ListQuery<TDto>
{
    [FromQuery(Name = "page")]
    public int? Page { get; set; }

    [FromQuery(Name = "per_page")]
    public int? PerPage { get; set; }

    [FromQuery(Name = "sort")]
    public string? Sort { get; set; }

    public PagingParams GetPagingParams() => new(Page, Math.Min(PerPage ?? 100, 500));

    public bool TryGetSortingParams(
        [NotNullWhen(true)] out SortingParams? sorting,
        [NotNullWhen(false)] out string? error) =>
        SortableDto.For<TDto>().TryParse(Sort, JsonNamingPolicy.SnakeCaseLower, out sorting, out error);
}

public sealed class GetOrdersQuery : ListQuery<OrderDto>
{
    [FromQuery(Name = "customer_id")]
    public int? CustomerId { get; set; }
}

app.MapGet("/orders", ([AsParameters] GetOrdersQuery query, ...) => ...);
```

In a controller, bind the same class with `[FromQuery]`.

## Without a database

`OrderBy(SortingParams)` works on any `IQueryable`, and `Paginate` pages a list in memory, for
values that are computed after loading:

```csharp
SortingParams sorting = SortingParams.Create().Descending("CreatedAt").Ascending("Id");

IOrderedQueryable<Order> sorted = orders.AsQueryable().OrderBy(sorting);
PagedData<OrderDto> page = orderDtos.Paginate(sorting, new PagingParams(1, 50));
PagedData<OrderRow> rows = page.Map(OrderRow.From);   // same paging metadata
```

With no sort keys, `OrderBy` sorts by an `Id` property. A key that names no property of the
sorted type throws `SortingParamNotFoundException`: keys from `SortableDto` name the DTO's
properties, so sort the projected DTO, not the entity, when their names differ.

## Limitations

* One level of properties: no `customer.name`.
* No maximum page size: `new PagingParams(1, int.MaxValue)` is `PagingParams.All`. Cap the
  client's value before building `PagingParams` if that matters.
* Offset paging (`OFFSET`/`LIMIT`), not keyset paging.

## Samples

[`samples/Orders.Api`](https://github.com/hlib-zinchenko/Hlibz.Paging/blob/main/samples/README.md)
is a plain minimal API on EF Core with SQLite in memory: paged and sorted orders from the
database, a bad sort key as a `400`, every order in one query with `PagingParams.All`, and
customers paged in memory, with a `.http` file for each request.

```bash
dotnet run --project samples/Orders.Api   # http://localhost:5390
```

## Related packages

- [Hlibz.Results](https://www.nuget.org/packages/Hlibz.Results): return a bad sort string as
  `Result.Invalid(new Dictionary<string, string[]> { ["sort"] = [error] })`, so it gets the same
  error body as every other failure.
- [Hlibz.MinimalApi.Endpoints](https://www.nuget.org/packages/Hlibz.MinimalApi.Endpoints):
  minimal API endpoints as classes, for list endpoints among the rest.
- [Hlibz.OpenApi.AspNetCore](https://www.nuget.org/packages/Hlibz.OpenApi.AspNetCore): OpenAPI
  transformers for Microsoft.AspNetCore.OpenApi.

## Versioning

Every package in this repository is released under one version, from one `vX.Y.Z` tag. EF Core
is declared as a minimum for each target framework (10.0.15 on .NET 10, 11.0.0 on .NET 11): a
newer patch of it needs no new release here, reference it in your app and NuGet uses it.

Release notes: [GitHub Releases](https://github.com/hlib-zinchenko/Hlibz.Paging/releases).

## License

MIT
