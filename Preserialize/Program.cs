using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<Store>(c => c.UseInMemoryDatabase("db"));
builder.Services.AddSingleton<ViewStore>();
builder.Services.AddTransient<ViewStoreUpdater>();

var app = builder.Build();

app.MapGet("/", (ctx) =>
{
    var store = ctx.RequestServices.GetRequiredService<ViewStore>();
    ctx.Response.Headers.ContentType = "application/json; charset=utf-8";
    return ctx.Response.WriteAsync(store.All);
});

app.MapGet("/{id}", (ctx) =>
{
    ctx.Response.Headers.ContentType = "application/json; charset=utf-8";

    var store = ctx.RequestServices.GetRequiredService<ViewStore>();
    if (!ctx.Request.RouteValues.TryGetValue("id", out var value))
    {
        return ctx.Response.WriteAsync("null");
    }

    var id = int.Parse(value.ToString());

    return ctx.Response.WriteAsync(store[id]);
});

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

public class ViewStore : ConcurrentDictionary<int, string>
{
    public string All { get; private set; } = "[]";

    public void CompileAll()
    {
        var builder = new StringBuilder();
        builder.Append('[');
        foreach (var value in Values)
        {
            builder.Append(value);
            builder.Append(',');
        }

        builder.Remove(builder.Length - 1, 1);
        builder.Append(']');
        All = builder.ToString();
    }
}

public record ProductView(int Id, string Name, List<StockView> StockViews);

public record StockView(int Id, string Description);

public class ViewStoreUpdater
{
    private readonly ViewStore _store;

    public ViewStoreUpdater(ViewStore store) => _store = store;

    public void AddProduct(Product product)
    {
        _store[product.Id] = JsonSerializer.Serialize(new ProductView(
            product.Id,
            product.Name,
            product.Stocks.Select(x => new StockView(x.Id, x.Description)).ToList()
        ));
        _store.CompileAll();
    }

    public void AddStock(Stock stock)
    {
        var product = JsonSerializer.Deserialize<ProductView>(_store[stock.ProductId]);

        product.StockViews.Add(new(stock.Id, stock.Description));

        _store[product.Id] = JsonSerializer.Serialize(product);
        _store.CompileAll();
    }

    public void UpdateStock(Stock stock)
    {
        var product = JsonSerializer.Deserialize<ProductView>(_store[stock.ProductId]);
        product.StockViews.RemoveAll(x => x.Id == stock.Id);
        product.StockViews.Add(new(stock.Id, stock.Description));
        _store[product.Id] = JsonSerializer.Serialize(product);
        _store.CompileAll();
    }
}