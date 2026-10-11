using System;
using Azure;
using Azure.Data.Tables;

namespace ST10467898CLDV6212POE.Models
{
    public class OrderEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "ORDERS";
        public string RowKey { get; set; }
        public string CustomerId { get; set; }
        public string ItemsJson { get; set; }
        public string Status { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}