using ContosoInventory.Server.Data;
using ContosoInventory.Server.Models;
using ContosoInventory.Server.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContosoInventory.Server.Tests.Repositories;

/// <summary>
/// Tests product repository operations against SQLite.
/// </summary>
public class ProductRepositoryTests
{
    /// <summary>
    /// Verifies a restock atomically updates stock and its timestamp.
    /// </summary>
    [Fact]
    public async Task TryRestockAsync_ValidQuantity_AtomicallyUpdatesStockAndTimestamp()
    {
        var fixture = await CreateDatabaseAsync(stockQuantity: 4);
        await using var connection = fixture.Connection;
        await using var context = fixture.Context;
        var repository = new ProductRepository(context);
        var updatedDate = DateTime.UtcNow;

        var updatedCount = await repository.TryRestockAsync(fixture.Product.Id, 3, updatedDate);
        var updatedProduct = await context.Products.AsNoTracking().SingleAsync();

        Assert.Equal(1, updatedCount);
        Assert.Equal(7, updatedProduct.StockQuantity);
        Assert.Equal(updatedDate, updatedProduct.LastUpdatedDate);
    }

    /// <summary>
    /// Verifies a restock that would overflow leaves stock unchanged.
    /// </summary>
    [Fact]
    public async Task TryRestockAsync_QuantityWouldOverflow_DoesNotUpdateStock()
    {
        var fixture = await CreateDatabaseAsync(stockQuantity: int.MaxValue);
        await using var connection = fixture.Connection;
        await using var context = fixture.Context;
        var repository = new ProductRepository(context);

        var updatedCount = await repository.TryRestockAsync(fixture.Product.Id, 1, DateTime.UtcNow);
        var product = await context.Products.AsNoTracking().SingleAsync();

        Assert.Equal(0, updatedCount);
        Assert.Equal(int.MaxValue, product.StockQuantity);
    }

    private static async Task<(SqliteConnection Connection, InventoryContext Context, Product Product)> CreateDatabaseAsync(
        int stockQuantity)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<InventoryContext>()
            .UseSqlite(connection)
            .Options;
        var context = new InventoryContext(options);
        await context.Database.EnsureCreatedAsync();

        var category = new Category
        {
            Name = "Test category",
            Description = "Category used in repository tests",
            CreatedDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Test product",
            Sku = "TEST-SKU",
            Description = "Product used in repository tests",
            Price = 10m,
            StockQuantity = stockQuantity,
            CategoryId = category.Id,
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        return (connection, context, product);
    }
}
