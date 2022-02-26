using Microsoft.EntityFrameworkCore;

namespace Shared;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<Stock> Stocks { get; set; } = new();
}

public class Stock
{
    public int Id { get; set; }
    public string Description { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; }
}

public class Store : DbContext
{
    public Store(DbContextOptions<Store> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Stock> Stocks { get; set; }
}