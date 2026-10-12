
using System;
using System.Globalization;
using System.Linq;
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
        private readonly TableServiceClient _tableServiceClient;

        public ProcessOrderQueue(
            ILogger<ProcessOrderQueue> logger,
            TableServiceClient tableServiceClient)
        {
            _logger = logger;
            _tableServiceClient = tableServiceClient;
        }

        [Function("ProcessOrderQueue")]
        public async Task Run(
            [QueueTrigger(
                "order-processing-queue",
                Connection = "AzureWebJobsStorage")]
            string rawMessage)
        {
            try
            {
                // Queue triggers normally provide decoded text.
                // Also handle Base64 text if supplied directly.
                string json = rawMessage;

                if (!rawMessage.TrimStart().StartsWith("{"))
                {
                    json = Encoding.UTF8.GetString(
                        Convert.FromBase64String(rawMessage));
                }

                var order =
                    JsonConvert.DeserializeObject<OrderQueueMessage>(json);

                if (order == null ||
                    string.IsNullOrWhiteSpace(order.OrderId) ||
                    !Guid.TryParse(order.OrderId, out _) ||
                    string.IsNullOrWhiteSpace(order.CustomerName) ||
                    order.SelectedItemSKUs == null ||
                    order.SelectedItemSKUs.Count == 0 ||
                    order.SelectedItemSKUs.Any(
                        x => string.IsNullOrWhiteSpace(x)) ||
                    order.TotalPrice <= 0 ||
                    order.OrderTimestamp == default)
                {
                    throw new InvalidOperationException(
                        "Invalid order message received.");
                }

                var tableClient =
                    _tableServiceClient.GetTableClient("Orders");

                await tableClient.CreateIfNotExistsAsync();

                var entity = new OrderEntity
                {
                    PartitionKey = order.OrderTimestamp.UtcDateTime
                        .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),

                    RowKey = order.OrderId,
                    CustomerName = order.CustomerName.Trim(),
                    ItemsJson = JsonConvert.SerializeObject(
                        order.SelectedItemSKUs),
                    TotalPrice = (double)order.TotalPrice,
                    OrderTimestamp = order.OrderTimestamp,
                    Status = "Received",
                    StatusHistory = "Received"
                };

                await tableClient.UpsertEntityAsync(
                    entity, TableUpdateMode.Replace);

                _logger.LogInformation(
                    "Order {OrderId} saved as Received.",
                    order.OrderId);

                // Processing starts after the record is received.
                entity.Status = "Preparing";
                entity.StatusHistory = "Received -> Preparing";

                await tableClient.UpsertEntityAsync(
                    entity, TableUpdateMode.Replace);

                _logger.LogInformation(
                    "Order {OrderId} updated to Preparing.",
                    order.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing queue message.");

                // Allow the queue runtime to retry failures.
                throw;
            }
        }
    }
}
