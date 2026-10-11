
using System.Net;
using System.Text;
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

        // STEP 1: Read and validate JSON
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

        // STEP 2: Validate customer name
        if (order == null ||
            string.IsNullOrWhiteSpace(order.CustomerName))
        {
            return await WriteError(
                req,
                HttpStatusCode.BadRequest,
                "CustomerName is required.");
        }

        // STEP 3: Validate selected menu items
        if (order.SelectedItemSKUs == null ||
            order.SelectedItemSKUs.Count == 0 ||
            order.SelectedItemSKUs.Any(
                sku => string.IsNullOrWhiteSpace(sku)))
        {
            return await WriteError(
                req,
                HttpStatusCode.BadRequest,
                "At least one valid menu item SKU is required.");
        }

        // STEP 4: Validate total price
        if (order.TotalPrice <= 0)
        {
            return await WriteError(
                req,
                HttpStatusCode.BadRequest,
                "TotalPrice must be greater than zero.");
        }

        // STEP 5: Generate order information
        var message = new
        {
            OrderId = Guid.NewGuid().ToString(),
            CustomerName = order.CustomerName.Trim(),
            SelectedItemSKUs = order.SelectedItemSKUs
                .Select(sku => sku.Trim())
                .ToList(),
            TotalPrice = order.TotalPrice,
            OrderTimestamp = DateTimeOffset.UtcNow
        };

        // STEP 6: Send the order to Azure Queue Storage
        try
        {
            await _queueClient.CreateIfNotExistsAsync();

            string messageJson = JsonSerializer.Serialize(message);

            // Base64 encoding for the Azure Queue trigger
            string encodedMessage = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(messageJson));

            await _queueClient.SendMessageAsync(encodedMessage);

            _logger.LogInformation(
                "Order {OrderId} queued successfully for {CustomerName}.",
                message.OrderId,
                message.CustomerName);

            // STEP 7: Return HTTP 202 Accepted
            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteAsJsonAsync(message);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send order to Azure Queue Storage.");

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

        await response.WriteAsJsonAsync(new
        {
            error = message
        });

        return response;
    }
}

// Order data received from Postman
public class CreateOrderRequest
{
    public string? CustomerName { get; set; }

    public List<string>? SelectedItemSKUs { get; set; }

    public decimal TotalPrice { get; set; }
}
