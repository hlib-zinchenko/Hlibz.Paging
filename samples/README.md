# Samples

## Orders.Api

A plain minimal API on EF Core with SQLite in memory, seeded at startup with 7 customers and 45
orders. It uses Hlibz.Paging and Hlibz.Paging.EntityFrameworkCore and nothing else from Hlibz:
query binding and the 400 for a bad sort string are a few lines in `Program.cs`, as they would
be in your API.

```bash
dotnet run --project samples/Orders.Api    # http://localhost:5390
```

[`Orders.Api.http`](Orders.Api/Orders.Api.http) has every request below.

| Request | Shows | Code |
|---|---|---|
| `GET /orders?per_page=10` | The first 10 of 45 orders, newest first: `OrderDto`'s `[Sortable(IsDefault = true, DefaultDirection = Descending)]`. `paging_metadata` says 5 pages. | `PaginateAsync` in `Program.cs`, `OrderDto` in `Dtos.cs` |
| `GET /orders?page=2&per_page=10&sort=customer_name,created_at:desc` | Sorted by a joined column, then newest first within each customer, in the database. | `ProjectOrders`: the query is projected to `OrderDto` before sorting, so keys name its properties |
| `GET /orders?per_page=5&sort=CustomerName:desc,createdAt` | Keys in any casing name the same properties. | `SortableDto.TryParse` |
| `GET /orders?sort=total` | A `400` problem whose `sort` error names what can be sorted by, in snake_case: `Can't sort by total. Sort by: id, customer_name, created_at.` | `TryParseSort` in `Program.cs` |
| `GET /orders/export?sort=id` | Every order in one query, with no `COUNT`: `PagingParams.All`. | `/orders/export` in `Program.cs` |
| `GET /customers?per_page=3&sort=country,name` | The same paging and sorting on a list in memory, no database. Strings sort case-insensitively: "anna schmidt" comes before "Bruno Costa". | `Paginate` in `Program.cs`, `CustomerDirectory` in `Dtos.cs` |
| `GET /customers?page=0&per_page=-5` | Out-of-range values fall back to page 1 and 100 per page. | `new PagingParams(page, perPage)` |

A DTO without exactly one default doesn't build (`HPAG001`). At startup,
`SortableDto.ValidateAll` checks again every DTO with a `[Sortable]` property in the app's
assembly, reading the list the source generator wrote, so such a DTO stops the app before any
request even where the build error is suppressed. The sample references the generator project as
an analyzer, which the package brings in your own app.

The sample targets net11.0 only, references the library projects and is never packed.
