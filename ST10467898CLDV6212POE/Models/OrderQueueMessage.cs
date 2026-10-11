using System;
using System.Collections.Generic;

namespace ST10467898CLDV6212POE.Models
{
    public class OrderItemDto
    {
        public string Sku { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderQueueMessage
    {
        public string OrderId { get; set; }
        public string CustomerId { get; set; }
        public List<OrderItemDto> Items { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; }
    }
}
