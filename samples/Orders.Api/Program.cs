using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Hlibz.Paging;
using Hlibz.Paging.EntityFrameworkCore;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using Orders.Api;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// snake_case JSON: bodies, sort keys and the sort error's list of keys all use the same names.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);

// SQLite in memory lives as long as its connection, so one connection is opened and kept open
// for the app's lifetime (the container disposes it on shutdown).
builder.Services.AddSingleton(_ =>
{
    SqliteConnection connection = new("DataSource=:memory:");
    connection.Open();
    return connection;
});
builder.Services.AddDbContext<ShopDb>((services, options) =>
    options.UseSqlite(services.GetRequiredService<SqliteConnection>()));

WebApplication app = builder.Build();

// Every DTO with a [Sortable] property must have exactly one default: checked here, at startup,
// rather than by the first request that sorts it.
SortableDto.ValidateAll([typeof(OrderDto).Assembly]);

await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    ShopDb db = scope.ServiceProvider.GetRequiredService<ShopDb>();
    await db.Database.EnsureCreatedAsync();
    await db.SeedAsync(CancellationToken.None);
}

// GET /orders?page=2&per_page=10&sort=customer_name,created_at:desc
// Sorted, counted and paged in the database. The sort keys name OrderDto's properties, so the
// query is projected to OrderDto first (customer_name comes from a join).
app.MapGet("/orders", async Task<Results<Ok<PagedData<OrderDto>>, ValidationProblem>> (
    int? page,
    [FromQuery(Name = "per_page")] int? perPage,
    string? sort,
    ShopDb db,
    CancellationToken cancellationToken) =>
{
    if (!TryParseSort<OrderDto>(sort, out SortingParams? sorting, out ValidationProblem? problem))
    {
        return problem;
    }

    PagedData<OrderDto> orders = await ProjectOrders(db)
        .PaginateAsync(sorting, new PagingParams(page, perPage), cancellationToken);
    return TypedResults.Ok(orders);
});

// GET /orders/export?sort=id
// Every order in one query, no COUNT: PagingParams.All, for an export, say.
app.MapGet("/orders/export", async Task<Results<Ok<PagedData<OrderDto>>, ValidationProblem>> (
    string? sort,
    ShopDb db,
    CancellationToken cancellationToken) =>
{
    if (!TryParseSort<OrderDto>(sort, out SortingParams? sorting, out ValidationProblem? problem))
    {
        return problem;
    }

    PagedData<OrderDto> orders = await ProjectOrders(db)
        .PaginateAsync(sorting, PagingParams.All, cancellationToken);
    return TypedResults.Ok(orders);
});

// GET /customers?page=1&per_page=3&sort=country,name
// The same paging and sorting on an in-memory list, with Paginate.
app.MapGet("/customers", Results<Ok<PagedData<CustomerDto>>, ValidationProblem> (
    int? page,
    [FromQuery(Name = "per_page")] int? perPage,
    string? sort) =>
{
    if (!TryParseSort<CustomerDto>(sort, out SortingParams? sorting, out ValidationProblem? problem))
    {
        return problem;
    }

    return TypedResults.Ok(CustomerDirectory.All.Paginate(sorting, new PagingParams(page, perPage)));
});

app.MapGet("/", () => Results.Redirect("/orders?per_page=10"));

await app.RunAsync();

// A sort string the DTO doesn't allow is the client's mistake: a 400 whose message names the
// keys it may use, in the API's own casing ("Can't sort by total. Sort by: id, ...").
static bool TryParseSort<TDto>(
    string? sort,
    [NotNullWhen(true)] out SortingParams? sorting,
    [NotNullWhen(false)] out ValidationProblem? problem)
{
    if (SortableDto.For<TDto>().TryParse(
            sort, JsonNamingPolicy.SnakeCaseLower, out sorting, out string? error))
    {
        problem = null;
        return true;
    }

    problem = TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["sort"] = [error] });
    return false;
}

static IQueryable<OrderDto> ProjectOrders(ShopDb db) =>
    db.Orders.Select(order => new OrderDto
    {
        Id = order.Id,
        CustomerName = order.Customer.Name,
        CreatedAt = order.CreatedAt,
        Total = order.Total,
    });
