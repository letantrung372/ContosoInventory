using ContosoInventory.Shared.DTOs;

namespace ContosoInventory.Server.Services;

/// <summary>
/// Defines operations for managing inventory products.
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Retrieves all products, optionally filtered by category.
    /// </summary>
    /// <param name="categoryId">Optional category identifier to filter by.</param>
    /// <returns>The matching products.</returns>
    Task<List<ProductResponseDto>> GetAllProductsAsync(int? categoryId = null);

    /// <summary>
    /// Retrieves a product by its identifier.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <returns>The product DTO, or null if not found.</returns>
    Task<ProductResponseDto?> GetProductByIdAsync(int id);

    /// <summary>
    /// Creates a product.
    /// </summary>
    /// <param name="dto">The product creation data.</param>
    /// <returns>The created product DTO.</returns>
    /// <exception cref="InvalidOperationException">The SKU is already in use or the category does not exist.</exception>
    Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto);

    /// <summary>
    /// Updates a product.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="dto">The updated product data.</param>
    /// <returns>The updated product DTO, or null if not found.</returns>
    /// <exception cref="InvalidOperationException">The SKU is already in use or the category does not exist.</exception>
    Task<ProductResponseDto?> UpdateProductAsync(int id, UpdateProductDto dto);

    /// <summary>
    /// Deletes a product.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <returns>True if the product was deleted; otherwise, false.</returns>
    Task<bool> DeleteProductAsync(int id);

    /// <summary>
    /// Adds stock to a product.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="dto">The quantity to add.</param>
    /// <returns>The updated product DTO, or null if not found.</returns>
    Task<ProductResponseDto?> RestockProductAsync(int id, RestockProductDto dto);
}