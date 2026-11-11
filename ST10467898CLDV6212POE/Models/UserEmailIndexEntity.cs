using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.ApiGateway.Models;

public class UserEmailIndexEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "EMAIL";
    public string RowKey { get; set; } = ""; // Normalized email
    public string UserPartitionKey { get; set; } = "Customer";
    public string UserRowKey { get; set; } = "";
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}