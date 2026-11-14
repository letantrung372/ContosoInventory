using ContosoInventory.Server.Models;
using ContosoInventory.Server.Repositories;
using ContosoInventory.Server.Services;
using ContosoInventory.Shared.DTOs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ContosoInventory.Server.Tests.Services;

/// <summary>
/// Tests product service validation and stock operations.
/// </summary>
public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly ProductService _service;

    /// <summary>
    /// Initializes a product service with a mocked repository.
    /// </summary>
    public ProductServiceTests()
    {
        _service = new ProductService(_productRepository.Object, Mock.Of<ILogger<ProductService>>());
    }

    /// <summary>
    /// Verifies invalid product data is rejected before repository changes.
    /// </summary>
    [Fact]
    public async Task CreateProductAsync_InvalidDto_ThrowsValidationException()
    {
        var dto = new CreateProductDto
        {
            Name = " ",
            Sku = "SKU-1",
            Description = "Test product",
            Price = 12.50m,
            StockQuantity = 4,
            CategoryId = 1
        };

        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
            () => _service.CreateProductAsync(dto));

        _productRepository.Verify(repository => repository.Add(It.IsAny<Product>()), Times.Never);
    }

    /// <summary>
    /// Verifies an existing SKU prevents product creation.
    /// </summary>
    [Fact]
    public async Task CreateProductAsync_DuplicateSku_ThrowsInvalidOperationException()
    {
        var dto = new CreateProductDto
        {
            Name = "Test product",
            Sku = "SKU-1",
            Description = "Test product description",
            Price = 12.50m,
            StockQuantity = 4,
            CategoryId = 1
        };
        _productRepository.Setup(repository => repository.CategoryExistsAsync(dto.CategoryId))
            .ReturnsAsync(true);
        _productRepository.Setup(repository => repository.SkuExistsAsync(dto.Sku, null))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateProductAsync(dto));

        _productRepository.Verify(repository => repository.Add(It.IsAny<Product>()), Times.Never);
    }

    /// <summary>
    /// Verifies a successful restock returns the updated stock quantity.
    /// </summary>
    [Fact]
    public async Task RestockProductAsync_ValidQuantity_UpdatesStockAndReturnsProduct()
    {
        const int productId = 5;
        const int quantityToAdd = 3;
        var product = new Product
        {
            Id = productId,
            Name = "Test product",
            Sku = "SKU-1",
            Description = "Test product description",
            Price = 12.50m,
            StockQuantity = 5,
            CategoryId = 1,
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow
        };
        var dto = new RestockProductDto { QuantityToAdd = quantityToAdd };

        _productRepository.Setup(repository => repository.TryRestockAsync(
                productId, quantityToAdd, It.IsAny<DateTime>()))
            .Callback<int, int, DateTime>((_, quantity, updatedDate) =>
            {
                product.StockQuantity += quantity;
                product.LastUpdatedDate = updatedDate;
            })
            .ReturnsAsync(1);
        _productRepository.Setup(repository => repository.GetByIdAsync(productId))
            .ReturnsAsync(product);

        var result = await _service.RestockProductAsync(productId, dto);

        Assert.NotNull(result);
        Assert.Equal(8, result.StockQuantity);
        _productRepository.Verify(repository => repository.TryRestockAsync(
            productId, quantityToAdd, It.IsAny<DateTime>()), Times.Once);
    }

    /// <summary>
    /// Verifies restocking beyond the supported quantity is rejected.
    /// </summary>
    [Fact]
    public async Task RestockProductAsync_QuantityWouldOverflow_ThrowsInvalidOperationException()
    {
        const int productId = 5;
        var dto = new RestockProductDto { QuantityToAdd = 1 };
        var product = new Product
        {
            Id = productId,
            Name = "Test product",
            Sku = "SKU-1",
            Description = "Test product description",
            Price = 12.50m,
            StockQuantity = int.MaxValue,
            CategoryId = 1
        };

        _productRepository.Setup(repository => repository.TryRestockAsync(
                productId, dto.QuantityToAdd, It.IsAny<DateTime>()))
            .ReturnsAsync(0);
        _productRepository.Setup(repository => repository.GetByIdAsync(productId))
            .ReturnsAsync(product);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RestockProductAsync(productId, dto));
    }
}
