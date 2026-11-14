using Microsoft.EntityFrameworkCore;
using ContosoInventory.Server.Data;
using ContosoInventory.Server.Models;

namespace ContosoInventory.Server.Repositories;

/// <summary>
/// Provides Entity Framework data access for inventory products.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly InventoryContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductRepository"/> class.
    /// </summary>
    public ProductRepository(InventoryContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public Task<List<Product>> GetAllAsync(int? categoryId)
    {
        var query = _context.Products.AsNoTracking();
        if (categoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == categoryId.Value);
        }

        return query.OrderBy(product => product.Name).ToListAsync();
    }

    /// <inheritdoc />
    public Task<Product?> GetByIdAsync(int id)
    {
        return _context.Products.AsNoTracking().FirstOrDefaultAsync(product => product.Id == id);
    }

    /// <inheritdoc />
    public Task<Product?> GetTrackedByIdAsync(int id)
    {
        return _context.Products.FirstOrDefaultAsync(product => product.Id == id);
    }

    /// <inheritdoc />
    public Task<bool> CategoryExistsAsync(int categoryId)
    {
        return _context.Categories.AnyAsync(category => category.Id == categoryId);
    }

    /// <inheritdoc />
    public Task<bool> SkuExistsAsync(string sku, int? excludedProductId = null)
    {
        return _context.Products.AnyAsync(product =>
            product.Sku == sku && (!excludedProductId.HasValue || product.Id != excludedProductId.Value));
    }

    /// <inheritdoc />
    public void Add(Product product)
    {
        _context.Products.Add(product);
    }

    /// <inheritdoc />
    public void Remove(Product product)
    {
        _context.Products.Remove(product);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public Task<int> TryRestockAsync(int id, int quantity, DateTime updatedDate)
    {
        return _context.Products
            .Where(product => product.Id == id && product.StockQuantity <= int.MaxValue - quantity)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(product => product.StockQuantity, product => product.StockQuantity + quantity)
                .SetProperty(product => product.LastUpdatedDate, updatedDate));
    }
}