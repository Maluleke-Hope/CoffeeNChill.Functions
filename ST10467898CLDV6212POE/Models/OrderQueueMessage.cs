
using System;
using System.Collections.Generic;

namespace ST10467898CLDV6212POE.Models
{
    public class OrderQueueMessage
    {
        public string? OrderId { get; set; }
        public string? CustomerName { get; set; }
        public List<string>? SelectedItemSKUs { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTimeOffset OrderTimestamp { get; set; }
    }
}
