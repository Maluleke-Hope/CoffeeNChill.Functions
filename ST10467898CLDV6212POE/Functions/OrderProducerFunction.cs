
using System.Net;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ST10467898CLDV6212POE.Functions;

public class OrderProducerFunction
{
    private readonly QueueClient _queueClient;
    private readonly ILogger<OrderProducerFunction> _logger;

    public OrderProducerFunction(
        QueueServiceClient queueServiceClient,
        ILogger<OrderProducerFunction> logger)
    {
        _queueClient = queueServiceClient.GetQueueClient(
            "order-processing-queue");

        _logger = logger;
    }

    [Function("QueueOrder")]
    public async Task<HttpResponseData> QueueOrder(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "post",
            Route = "orders/queue")]
        HttpRequestData req)
    {
        CreateOrderRequest? order;

        try
        {
            order = await JsonSerializer.DeserializeAsync<CreateOrderRequest>(
                req.Body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch (JsonException)
        {
            return await WriteError(
                req,
                HttpStatusCode.BadRequest,
                "Invalid JSON format.");
        }

        if (order == null ||
            string.IsNullOrWhiteSpace(order.CustomerId) ||
            order.Items == null ||
            order.Items.Count == 0 ||
            order.Items.Any(item =>
                string.IsNullOrWhiteSpace(item.Sku) ||
                item.Quantity <= 0))
        {
            return await WriteError(
                req,
                HttpStatusCode.BadRequest,
                "A customerId and at least one valid item are required.");
        }

        var message = new
        {
            orderId = Guid.NewGuid().ToString(),
            customerId = order.CustomerId.Trim(),
            items = order.Items.Select(item => new
            {
                sku = item.Sku!.Trim(),
                quantity = item.Quantity
            }).ToList(),
            timestamp = DateTimeOffset.UtcNow,
            status = "Received"
        };

        try
        {
            await _queueClient.CreateIfNotExistsAsync();

            string messageJson = JsonSerializer.Serialize(message);

            // Encode the JSON for Azure Queue Storage.
            string encodedMessage = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(messageJson));

            await _queueClient.SendMessageAsync(encodedMessage);

            _logger.LogInformation(
                "Order {OrderId} queued successfully.",
                message.orderId);

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteAsJsonAsync(message);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue order.");

            return await WriteError(
                req,
                HttpStatusCode.InternalServerError,
                "Unable to queue the order.");
        }
    }

    private static async Task<HttpResponseData> WriteError(
        HttpRequestData req,
        HttpStatusCode status,
        string message)
    {
        var response = req.CreateResponse(status);
        await response.WriteAsJsonAsync(new { error = message });
        return response;
    }
}

public class CreateOrderRequest
{
    public string? CustomerId { get; set; }
    public List<OrderItemRequest>? Items { get; set; }
}

public class OrderItemRequest
{
    public string? Sku { get; set; }
    public int Quantity { get; set; }
}
