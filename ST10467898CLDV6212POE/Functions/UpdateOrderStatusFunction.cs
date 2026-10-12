
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ST10467898CLDV6212POE.Models;

namespace ST10467898CLDV6212POE.Functions;

public class UpdateOrderStatusFunction
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly ILogger<UpdateOrderStatusFunction> _logger;

    public UpdateOrderStatusFunction(
        TableServiceClient tableServiceClient,
        ILogger<UpdateOrderStatusFunction> logger)
    {
        _tableServiceClient = tableServiceClient;
        _logger = logger;
    }

    [Function("UpdateOrderStatus")]
    public async Task<HttpResponseData> UpdateOrderStatus(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "put",
            Route = "orders/{orderDate}/{orderId}/status")]
        HttpRequestData req,
        string orderDate,
        string orderId)
    {
        if (!DateTime.TryParseExact(
            orderDate,
            "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out _) ||
            !Guid.TryParse(orderId, out _))
        {
            return await Error(
                req,
                HttpStatusCode.BadRequest,
                "Invalid order date or order ID.");
        }

        UpdateOrderStatusRequest? body;

        try
        {
            body = await JsonSerializer.DeserializeAsync<
                UpdateOrderStatusRequest>(
                req.Body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch (JsonException)
        {
            return await Error(
                req,
                HttpStatusCode.BadRequest,
                "Invalid JSON format.");
        }

        string requestedStatus = body?.Status?.Trim() ?? "";

        if (requestedStatus != "Ready" &&
            requestedStatus != "Collected")
        {
            return await Error(
                req,
                HttpStatusCode.BadRequest,
                "Allowed status updates are Ready and Collected.");
        }

        try
        {
            var table = _tableServiceClient.GetTableClient("Orders");

            var result = await table.GetEntityIfExistsAsync<OrderEntity>(
                orderDate, orderId);

            if (!result.HasValue)
            {
                return await Error(
                    req,
                    HttpStatusCode.NotFound,
                    "Order not found.");
            }

            OrderEntity entity = result.Value;

            bool validTransition =
                (entity.Status == "Preparing" &&
                 requestedStatus == "Ready") ||
                (entity.Status == "Ready" &&
                 requestedStatus == "Collected");

            if (!validTransition)
            {
                return await Error(
                    req,
                    HttpStatusCode.Conflict,
                    $"Cannot change order status from " +
                    $"{entity.Status} to {requestedStatus}.");
            }

            entity.Status = requestedStatus;
            entity.StatusHistory =
                $"{entity.StatusHistory} -> {requestedStatus}";

            // Optimistic concurrency protects against simultaneous updates.
            await table.UpdateEntityAsync(
                entity,
                result.Value.ETag,
                TableUpdateMode.Replace);

            _logger.LogInformation(
                "Order {OrderId} updated to {Status}.",
                orderId,
                requestedStatus);

            var response = req.CreateResponse(HttpStatusCode.OK);

            await response.WriteAsJsonAsync(new
            {
                OrderId = orderId,
                Status = entity.Status,
                StatusHistory = entity.StatusHistory
            });

            return response;
        }
        catch (RequestFailedException ex) when (ex.Status == 412)
        {
            return await Error(
                req,
                HttpStatusCode.Conflict,
                "Order was updated by another request. Try again.");
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Order status storage error.");

            return await Error(
                req,
                HttpStatusCode.InternalServerError,
                "Unable to update order status.");
        }
    }

    private static async Task<HttpResponseData> Error(
        HttpRequestData req,
        HttpStatusCode status,
        string message)
    {
        var response = req.CreateResponse(status);
        await response.WriteAsJsonAsync(new { error = message });
        return response;
    }
}

public class UpdateOrderStatusRequest
{
    public string? Status { get; set; }
}
