using System;
using System.Text;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ST10467898CLDV6212POE.Models;

namespace ST10467898CLDV6212POE
{
    public class ProcessOrderQueue
    {
        private readonly ILogger<ProcessOrderQueue> _logger;

        public ProcessOrderQueue(ILogger<ProcessOrderQueue> logger)
        {
            _logger = logger;
        }

        [Function("ProcessOrderQueue")]
        public async Task Run(
            [QueueTrigger("order-processing-queue", Connection = "AzureWebJobsStorage")] string rawMessage)
        {
            _logger.LogInformation($"[ProcessOrderQueue] Received message from queue.");

            try
            {
                string jsonString = rawMessage;
                if (IsBase64String(rawMessage))
                {
                    byte[] data = Convert.FromBase64String(rawMessage);
                    jsonString = Encoding.UTF8.GetString(data);
                }

                var order = JsonConvert.DeserializeObject<OrderQueueMessage>(jsonString);

                if (order == null || string.IsNullOrEmpty(order.OrderId))
                {
                    _logger.LogWarning("[ProcessOrderQueue] Invalid order payload received.");
                    return;
                }

                _logger.LogInformation($"[ProcessOrderQueue] Processing Order ID: {order.OrderId} for Customer: {order.CustomerId}");

                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                    ?? "UseDevelopmentStorage=true";

                var tableClient = new TableClient(connectionString, "Orders");
                await tableClient.CreateIfNotExistsAsync();

                var orderEntity = new OrderEntity
                {
                    PartitionKey = "ORDERS",
                    RowKey = order.OrderId,
                    CustomerId = order.CustomerId,
                    ItemsJson = JsonConvert.SerializeObject(order.Items),
                    Status = "Preparing",
                    Timestamp = DateTimeOffset.UtcNow
                };

                await tableClient.UpsertEntityAsync(orderEntity);

                _logger.LogInformation($"[ProcessOrderQueue] Successfully saved Order {order.OrderId} with status 'Preparing' into Orders table.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ProcessOrderQueue] Error processing order: {ex.Message}");
                throw;
            }
        }

        private bool IsBase64String(string base64)
        {
            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out _);
        }
    }
}