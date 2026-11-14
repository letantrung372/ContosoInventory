using ContosoInventory.Server.Models;

namespace ContosoInventory.Server.Repositories;

/// <summary>
/// Defines data access operations for inventory products.
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Retrieves products, optionally filtered by category.
    /// </summary>
    Task<List<Product>> GetAllAsync(int? categoryId);

    /// <summary>
    /// Retrieves a product by its identifier without tracking changes.
    /// </summary>
    Task<Product?> GetByIdAsync(int id);

    /// <summary>
    /// Retrieves a tracked product by its identifier.
    /// </summary>
    Task<Product?> GetTrackedByIdAsync(int id);

    /// <summary>
    /// Checks whether a category exists.
    /// </summary>
    Task<bool> CategoryExistsAsync(int categoryId);

    /// <summary>
    /// Checks whether a SKU is assigned to another product.
    /// </summary>
    Task<bool> SkuExistsAsync(string sku, int? excludedProductId = null);

    /// <summary>
    /// Adds a product to the unit of work.
    /// </summary>
    void Add(Product product);

    /// <summary>
    /// Removes a product from the unit of work.
    /// </summary>
    void Remove(Product product);

    /// <summary>
    /// Persists pending changes.
    /// </summary>
    Task SaveChangesAsync();

    /// <summary>
    /// Atomically increases stock if the resulting quantity fits in an integer.
    /// </summary>
    Task<int> TryRestockAsync(int id, int quantity, DateTime updatedDate);
}