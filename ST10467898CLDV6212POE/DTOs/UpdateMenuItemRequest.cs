namespace ST10467898CLDV6212POE.DTOs;

public class UpdateMenuItemRequest
{
    // Updated menu item name.
    public string Name { get; set; } = string.Empty;

    // Updated menu item description.
    public string Description { get; set; } = string.Empty;

    // Updated selling price.
    public double Price { get; set; }

    // Updated availability status.
    public bool IsAvailable { get; set; }
}