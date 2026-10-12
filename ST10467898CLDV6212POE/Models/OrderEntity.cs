
using System;
using Azure;
using Azure.Data.Tables;

namespace ST10467898CLDV6212POE.Models
{
    public class OrderEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;
        public string ItemsJson { get; set; } = "[]";
        public double TotalPrice { get; set; }

        public DateTimeOffset OrderTimestamp { get; set; }
        public string Status { get; set; } = "Received";
        public string StatusHistory { get; set; } = "Received";

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
