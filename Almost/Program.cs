using Microsoft.EntityFrameworkCore;
using Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<Store>(c => c.UseInMemoryDatabase("db"));

var app = builder.Build();

app.MapGet("/", (Store store) => store.Products
    .AsNoTracking()
    .Select(x => new
    {
        x.Id,
        x.Name,
        Stock = x.Stocks.AsQueryable().Select(s => new { s.Id, s.Description, }).ToList(),
    })
    .ToListAsync()
);

app.MapGet("/{id}", (int id, Store store) => store.Products
    .AsNoTracking()
    .Where(x => x.Id == id)
    .Select(x => new
    {
        x.Id,
        x.Name,
        Stock = x.Stocks.AsQueryable().Select(s => new { s.Id, s.Description, }).ToList(),
    })
    .FirstOrDefaultAsync()
);

app.MapPost("/products", async (Store store) =>
{
    Product product = new() { Name = Guid.NewGuid().ToString() };
    store.Products.Add(product);
    await store.SaveChangesAsync();
});

app.MapPost("/products/{id}/stock", async (int id, Store store) =>
{
    Stock stock = new() { ProductId = id, Description = Guid.NewGuid().ToString() };
    store.Stocks.Add(stock);
    await store.SaveChangesAsync();
});

app.Run();