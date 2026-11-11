using Azure;
using Azure.Data.Tables;

namespace ST10467898CLDV6212POE.Models;

public class UserEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty; // Role: "Admin", "Staff", or "Customer"
    public string RowKey { get; set; } = string.Empty;        // Username / Email
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? PartitionKeyOverride { get; set; }
    public string? RowKeyOverride { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}