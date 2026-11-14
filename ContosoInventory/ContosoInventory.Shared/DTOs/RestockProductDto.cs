namespace ContosoInventory.Shared.DTOs;

/// <summary>
/// Contains the quantity to add when restocking a product.
/// </summary>
public class RestockProductDto
{
    [Range(1, int.MaxValue)]
    public int QuantityToAdd { get; set; }
}