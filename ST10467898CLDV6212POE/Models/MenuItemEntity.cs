using Azure;
using Azure.Data.Tables;

namespace ST10467898CLDV6212POE.Models;

public class MenuItemEntity : ITableEntity
{
    // The menu category is stored as the PartitionKey.
    public string PartitionKey { get; set; } = string.Empty;

    // The unique menu item identifier is stored as the RowKey.
    public string RowKey { get; set; } = string.Empty;

    // The name of the menu item.
    public string Name { get; set; } = string.Empty;

    // A short description of the menu item.
    public string Description { get; set; } = string.Empty;

    // The price of the menu item.
    public double Price { get; set; }

    // Indicates whether the menu item is available.
    public bool IsAvailable { get; set; }

    // Required by ITableEntity.
    public ETag ETag { get; set; }

    // Timestamp supplied by Azure Table Storage.
    public DateTimeOffset? Timestamp { get; set; }
}