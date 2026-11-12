using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Hlibz.Paging.EntityFrameworkCore.Tests;

public sealed class PaginateAsyncTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    private ShopContext _db = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await _connection.OpenAsync(Ct);
        _db = new ShopContext(new DbContextOptionsBuilder<ShopContext>().UseSqlite(_connection).Options);
        await _db.Database.EnsureCreatedAsync(Ct);
        _db.Orders.AddRange(
            new Order { Id = 3, CustomerName = "banana", CreatedAt = Day(3) },
            new Order { Id = 1, CustomerName = "Apple", CreatedAt = Day(1) },
            new Order { Id = 5, CustomerName = "cherry", CreatedAt = Day(5) },
            new Order { Id = 2, CustomerName = "apricot", CreatedAt = Day(2) },
            new Order { Id = 4, CustomerName = "Blueberry", CreatedAt = Day(4) });
        await _db.SaveChangesAsync(Ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task PaginateAsync_WithPage_SortsInTheDatabaseThenPages()
    {
        PagedData<Order> page = await _db.Orders.PaginateAsync(
            SortingParams.Create().Ascending("customer_name"),
            new PagingParams(2, 2),
            Ct);

        Assert.Equal([3, 4], page.Data.Select(order => order.Id));
        Assert.Equal(5, page.PagingMetadata.TotalItems);
        Assert.Equal(3, page.PagingMetadata.TotalPages);
    }

    [Fact]
    public async Task PaginateAsync_WithProjection_SortsByDtoProperties()
    {
        PagedData<OrderDto> page = await _db.Orders
            .Where(order => order.Id != 5)
            .Select(order => new OrderDto { Id = order.Id, Customer = order.CustomerName, PlacedAt = order.CreatedAt })
            .PaginateAsync(SortingParams.Create().Descending("placedAt"), new PagingParams(1, 3), Ct);

        Assert.Equal([4, 3, 2], page.Data.Select(order => order.Id));
        Assert.Equal(4, page.PagingMetadata.TotalItems);
        Assert.True(page.PagingMetadata.HasNextPage);
    }

    [Fact]
    public async Task PaginateAsync_WithNoMatches_ReturnsEmptyPage()
    {
        PagedData<Order> page = await _db.Orders
            .Where(order => order.Id > 100)
            .PaginateAsync(SortingParams.Create(), new PagingParams(1, 10), Ct);

        Assert.Empty(page.Data);
        Assert.Equal(0, page.PagingMetadata.TotalItems);
    }

    [Fact]
    public async Task PaginateAsync_WithAll_LoadsEverythingSorted()
    {
        PagedData<Order> page = await _db.Orders.PaginateAsync(
            SortingParams.Create().Descending("Id"),
            PagingParams.All,
            Ct);

        Assert.Equal([5, 4, 3, 2, 1], page.Data.Select(order => order.Id));
        Assert.Equal(5, page.PagingMetadata.TotalItems);
    }

    [Fact]
    public async Task PaginateAsync_WithUnknownProperty_Throws()
    {
        await Assert.ThrowsAsync<SortingParamNotFoundException>(() =>
            _db.Orders.PaginateAsync(SortingParams.Create().Ascending("price"), new PagingParams(1, 10), Ct));
    }

    private static DateTime Day(int day) => new(2026, 1, day, 0, 0, 0, DateTimeKind.Utc);

    public sealed class Order
    {
        public int Id { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }

    public sealed class OrderDto
    {
        public int Id { get; init; }

        public string Customer { get; init; } = string.Empty;

        public DateTime PlacedAt { get; init; }
    }

    private sealed class ShopContext(DbContextOptions<ShopContext> options) : DbContext(options)
    {
        public DbSet<Order> Orders => Set<Order>();
    }
}
