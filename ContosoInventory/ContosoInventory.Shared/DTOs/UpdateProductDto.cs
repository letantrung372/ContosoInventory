namespace ContosoInventory.Shared.DTOs;

/// <summary>
/// Contains the editable data for an existing product.
/// </summary>
public class UpdateProductDto
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    [RegularExpression(@".*\S.*")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    public string Sku { get; set; } = string.Empty;

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}