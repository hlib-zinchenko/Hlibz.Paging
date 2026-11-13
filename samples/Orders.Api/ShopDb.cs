using Microsoft.EntityFrameworkCore;

namespace Orders.Api;

internal sealed class ShopDb(DbContextOptions<ShopDb> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Order> Orders => Set<Order>();

    /// <summary>The customers from <see cref="CustomerDirectory"/>, and 45 orders spread across them.</summary>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        Customers.AddRange(CustomerDirectory.All.Select(customer => new Customer
        {
            Id = customer.Id,
            Name = customer.Name,
            Country = customer.Country,
        }));

        DateTime start = new(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        Orders.AddRange(Enumerable.Range(1, 45).Select(id => new Order
        {
            Id = id,
            CustomerId = CustomerDirectory.All[(id * 5) % CustomerDirectory.All.Count].Id,
            CreatedAt = start.AddHours((id * 31) % 600),
            Total = 10m + ((id * 37) % 190) + ((id % 4) * 0.25m),
        }));

        await SaveChangesAsync(cancellationToken);
    }
}

internal sealed class Customer
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Country { get; set; }
}

internal sealed class Order
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public decimal Total { get; set; }
}
