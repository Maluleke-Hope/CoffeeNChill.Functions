namespace ST10467898CLDV6212POE.DTOs;

public class CreateMenuItemRequest
{
    // The category of the menu item.
    // This will be used as the Azure Table PartitionKey.
    public string Category { get; set; } = string.Empty;

    // The unique stock keeping unit.
    // This will be used as the Azure Table RowKey.
    public string SKU { get; set; } = string.Empty;

    // The name of the menu item.
    public string Name { get; set; } = string.Empty;

    // A short description of the menu item.
    public string Description { get; set; } = string.Empty;

    // The selling price of the menu item.
    public double Price { get; set; }

    // Indicates whether the menu item is currently available.
    public bool IsAvailable { get; set; }
}