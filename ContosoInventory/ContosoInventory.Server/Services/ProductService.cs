using System.ComponentModel.DataAnnotations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ContosoInventory.Server.Models;
using ContosoInventory.Server.Repositories;
using ContosoInventory.Shared.DTOs;

namespace ContosoInventory.Server.Services;

/// <summary>
/// Provides operations for managing inventory products.
/// </summary>
public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductService"/> class.
    /// </summary>
    public ProductService(IProductRepository productRepository, ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<ProductResponseDto>> GetAllProductsAsync(int? categoryId = null)
    {
        if (categoryId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(categoryId), "Category ID must be positive.");
        }

        try
        {
            var products = await _productRepository.GetAllAsync(categoryId);
            return products.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products for category {CategoryId}.", categoryId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto?> GetProductByIdAsync(int id)
    {
        ValidateId(id);

        try
        {
            var product = await _productRepository.GetByIdAsync(id);
            return product == null ? null : MapToDto(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product with ID {ProductId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto)
    {
        ValidateDto(dto);

        try
        {
            await EnsureCategoryExistsAsync(dto.CategoryId);
            await EnsureSkuAvailableAsync(dto.Sku.Trim());

            var now = DateTime.UtcNow;
            var product = new Product
            {
                Name = dto.Name.Trim(),
                Sku = dto.Sku.Trim(),
                Description = dto.Description.Trim(),
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                CategoryId = dto.CategoryId,
                CreatedDate = now,
                LastUpdatedDate = now
            };

            _productRepository.Add(product);
            await _productRepository.SaveChangesAsync();
            _logger.LogInformation("Product created: {ProductSku} (ID: {ProductId}).", product.Sku, product.Id);
            return MapToDto(product);
        }
        catch (DbUpdateException ex) when (IsSkuUniqueViolation(ex))
        {
            _logger.LogWarning(ex, "A product with SKU {ProductSku} already exists.", dto.Sku);
            throw new InvalidOperationException($"A product with SKU '{dto.Sku}' already exists.", ex);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product with SKU {ProductSku}.", dto.Sku);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto?> UpdateProductAsync(int id, UpdateProductDto dto)
    {
        ValidateId(id);
        ValidateDto(dto);

        try
        {
            var product = await _productRepository.GetTrackedByIdAsync(id);
            if (product == null)
            {
                return null;
            }

            await EnsureCategoryExistsAsync(dto.CategoryId);
            await EnsureSkuAvailableAsync(dto.Sku.Trim(), id);

            product.Name = dto.Name.Trim();
            product.Sku = dto.Sku.Trim();
            product.Description = dto.Description.Trim();
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;
            product.CategoryId = dto.CategoryId;
            product.LastUpdatedDate = DateTime.UtcNow;

            await _productRepository.SaveChangesAsync();
            _logger.LogInformation("Product updated: {ProductSku} (ID: {ProductId}).", product.Sku, product.Id);
            return MapToDto(product);
        }
        catch (DbUpdateException ex) when (IsSkuUniqueViolation(ex))
        {
            _logger.LogWarning(ex, "A product with SKU {ProductSku} already exists.", dto.Sku);
            throw new InvalidOperationException($"A product with SKU '{dto.Sku}' already exists.", ex);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product with ID {ProductId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteProductAsync(int id)
    {
        ValidateId(id);

        try
        {
            var product = await _productRepository.GetTrackedByIdAsync(id);
            if (product == null)
            {
                return false;
            }

            _productRepository.Remove(product);
            await _productRepository.SaveChangesAsync();
            _logger.LogInformation("Product deleted: {ProductSku} (ID: {ProductId}).", product.Sku, product.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product with ID {ProductId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto?> RestockProductAsync(int id, RestockProductDto dto)
    {
        ValidateId(id);
        ValidateDto(dto);

        try
        {
            var updated = await _productRepository.TryRestockAsync(id, dto.QuantityToAdd, DateTime.UtcNow);
            if (updated == 0)
            {
                var existingProduct = await _productRepository.GetByIdAsync(id);
                if (existingProduct == null)
                {
                    return null;
                }

                throw new InvalidOperationException("The restock quantity exceeds the maximum stock value.");
            }

            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return null;
            }

            _logger.LogInformation("Product restocked: {ProductSku} (ID: {ProductId}) by {Quantity} units.",
                product.Sku, product.Id, dto.QuantityToAdd);
            return MapToDto(product);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restocking product with ID {ProductId}.", id);
            throw;
        }
    }

    private async Task EnsureCategoryExistsAsync(int categoryId)
    {
        if (!await _productRepository.CategoryExistsAsync(categoryId))
        {
            throw new InvalidOperationException($"Category with ID {categoryId} does not exist.");
        }
    }

    private async Task EnsureSkuAvailableAsync(string sku, int? excludedProductId = null)
    {
        if (await _productRepository.SkuExistsAsync(sku, excludedProductId))
        {
            throw new InvalidOperationException($"A product with SKU '{sku}' already exists.");
        }
    }

    private static void ValidateDto(object dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);
    }

    private static void ValidateId(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Product ID must be positive.");
        }
    }

    private static bool IsSkuUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqliteException
        {
            SqliteErrorCode: 19,
            SqliteExtendedErrorCode: 2067
        };
    }

    private static ProductResponseDto MapToDto(Product product)
    {
        return new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Sku = product.Sku,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            CategoryId = product.CategoryId,
            CreatedDate = product.CreatedDate,
            LastUpdatedDate = product.LastUpdatedDate
        };
    }
}