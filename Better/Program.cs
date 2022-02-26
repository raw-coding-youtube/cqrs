using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<Store>(c => c.UseInMemoryDatabase("db"));
builder.Services.AddSingleton<ViewStore>();
builder.Services.AddTransient<ViewStoreUpdater>();

var app = builder.Build();

app.MapGet("/", (ViewStore store) => store.Values);

app.MapGet("/{id}", (int id, ViewStore store) => store.ContainsKey(id) ? store[id] : null);

app.MapPost("/products", async (Store store, ViewStoreUpdater updater) =>
{
    Product product = new() { Name = Guid.NewGuid().ToString() };
    store.Products.Add(product);
    await store.SaveChangesAsync();
    updater.AddProduct(product);
});

app.MapPost("/products/{id}/stock", async (int id, Store store, ViewStoreUpdater updater) =>
{
    Stock stock = new() { ProductId = id, Description = Guid.NewGuid().ToString() };
    store.Stocks.Add(stock);
    await store.SaveChangesAsync();
    updater.AddStock(stock);
});

app.MapPut("/stock", async (int id, string desc, Store store, ViewStoreUpdater updater) =>
{
    var stock = await store.Stocks.FirstOrDefaultAsync(x => x.Id == id);
    stock.Description = desc;
    await store.SaveChangesAsync();
    updater.UpdateStock(stock);
});

app.Run();

public class ViewStore : ConcurrentDictionary<int, ProductView>
{
}

public record ProductView(int Id, string Name, IEnumerable<StockView> StockViews);

public record StockView(int Id, string Description);

public class ViewStoreUpdater
{
    private readonly ViewStore _store;

    public ViewStoreUpdater(ViewStore store) => _store = store;

    public void AddProduct(Product product)
    {
        _store[product.Id] = new(
            product.Id,
            product.Name,
            product.Stocks.Select(x => new StockView(x.Id, x.Description)).ToImmutableArray()
        );
    }

    public void AddStock(Stock stock)
    {
        var product = _store[stock.ProductId];

        _store[stock.ProductId] = product with
        {
            StockViews = product.StockViews
                .Append(new(stock.Id, stock.Description))
                .ToImmutableArray(),
        };
    }

    public void UpdateStock(Stock stock)
    {
        var product = _store[stock.ProductId];

        _store[stock.ProductId] = product with
        {
            StockViews = product.StockViews
                .Where(x => x.Id != stock.Id)
                .Append(new(stock.Id, stock.Description))
                .ToImmutableArray()
        };
    }
}