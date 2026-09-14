using System.Net;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ST10467898CLDV6212POE.DTOs;
using ST10467898CLDV6212POE.Models;

namespace ST10467898CLDV6212POE.Functions;

public class MenuItemFunctions
{
    private readonly TableClient _tableClient;
    private readonly ILogger<MenuItemFunctions> _logger;

    private const string TableName = "MenuItems";

    public MenuItemFunctions(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<MenuItemFunctions>();

        string? connectionString =
            Environment.GetEnvironmentVariable("AzureWebJobsStorage");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "AzureWebJobsStorage connection string is not configured.");
        }

        TableServiceClient tableServiceClient =
            new TableServiceClient(connectionString);

        _tableClient = tableServiceClient.GetTableClient(TableName);

        _tableClient.CreateIfNotExists();
    }


    // ============================================================
    // CREATE
    // POST /api/menu
    // ============================================================

    [Function("CreateMenuItem")]
    public async Task<HttpResponseData> CreateMenuItem(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "post",
            Route = "menu")]
        HttpRequestData req)
    {
        try
        {
            CreateMenuItemRequest? request =
                await JsonSerializer.DeserializeAsync<CreateMenuItemRequest>(
                    req.Body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (request == null)
            {
                return await Error(
                    req,
                    HttpStatusCode.BadRequest,
                    "Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Category))
            {
                return await Error(
                    req,
                    HttpStatusCode.BadRequest,
                    "Category is required.");
            }

            if (string.IsNullOrWhiteSpace(request.SKU))
            {
                return await Error(
                    req,
                    HttpStatusCode.BadRequest,
                    "SKU is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return await Error(
                    req,
                    HttpStatusCode.BadRequest,
                    "Name is required.");
            }

            if (request.Price < 0)
            {
                return await Error(
                    req,
                    HttpStatusCode.BadRequest,
                    "Price cannot be negative.");
            }

            MenuItemEntity entity = new MenuItemEntity
            {
                PartitionKey = request.Category.Trim(),
                RowKey = request.SKU.Trim(),
                Name = request.Name.Trim(),
                Description = request.Description?.Trim() ?? "",
                Price = request.Price,
                IsAvailable = request.IsAvailable
            };

            try
            {
                await _tableClient.AddEntityAsync(entity);
            }
            catch (RequestFailedException ex)
                when (ex.Status == 409)
            {
                return await Error(
                    req,
                    HttpStatusCode.Conflict,
                    "A menu item with this Category and SKU already exists.");
            }

            HttpResponseData response =
                req.CreateResponse(HttpStatusCode.Created);

            await response.WriteAsJsonAsync(entity);

            return response;
        }
        catch (JsonException)
        {
            return await Error(
                req,
                HttpStatusCode.BadRequest,
                "Invalid JSON request body.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating menu item.");

            return await Error(
                req,
                HttpStatusCode.InternalServerError,
                ex.Message);
        }
    }


    // ============================================================
    // GET ALL
    // GET /api/menu
    // ============================================================

    [Function("GetAllMenuItems")]
    public async Task<HttpResponseData> GetAllMenuItems(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "menu")]
        HttpRequestData req)
    {
        try
        {
            List<MenuItemEntity> items = new List<MenuItemEntity>();

            await foreach (
                MenuItemEntity item
                in _tableClient.QueryAsync<MenuItemEntity>())
            {
                items.Add(item);
            }

            HttpResponseData response =
                req.CreateResponse(HttpStatusCode.OK);

            await response.WriteAsJsonAsync(items);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting menu items.");

            return await Error(
                req,
                HttpStatusCode.InternalServerError,
                ex.Message);
        }
    }


    // ============================================================
    // GET BY CATEGORY
    // GET /api/menu/category/{category}
    // ============================================================

    [Function("GetMenuItemsByCategory")]
    public async Task<HttpResponseData> GetMenuItemsByCategory(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "menu/category/{category}")]
        HttpRequestData req,
        string category)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return await Error(
                    req,
                    HttpStatusCode.BadRequest,
                    "Category is required.");
            }

            category = category.Trim();

            List<MenuItemEntity> items = new List<MenuItemEntity>();

            await foreach (
                MenuItemEntity item
                in _tableClient.QueryAsync<MenuItemEntity>(
                    x => x.PartitionKey == category))
            {
                items.Add(item);
            }

            HttpResponseData response =
                req.CreateResponse(HttpStatusCode.OK);

            await response.WriteAsJsonAsync(items);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category.");

            return await Error(
                req,
                HttpStatusCode.InternalServerError,
                ex.Message);
        }
    }


    // ============================================================
    // GET ONE
    // GET /api/menu/{category}/{sku}
    // ============================================================

    [Function("GetMenuItem")]
    public async Task<HttpResponseData> GetMenuItem(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "menu/{category}/{sku}")]
        HttpRequestData req,
        string category,
        string sku)
    {
        try
        {
            category = category.Trim();
            sku = sku.Trim();

            MenuItemEntity item;

            try
            {
                item = await _tableClient.GetEntityAsync<MenuItemEntity>(
                    category,
                    sku);
            }
            catch (RequestFailedException ex)
                when (ex.Status == 404)
            {
                return await Error(
                    req,
                    HttpStatusCode.NotFound,
                    "Menu item was not found.");
            }

            HttpResponseData response =
                req.CreateResponse(HttpStatusCode.OK);

            await response.WriteAsJsonAsync(item);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting menu item.");

            return await Error(
                req,
                HttpStatusCode.InternalServerError,
                ex.Message);
        }
    }


    // ============================================================
    // UPDATE
    // PUT /api/menu/{category}/{sku}
    // ============================================================

    [Function("UpdateMenuItem")]
    public async Task<HttpResponseData> UpdateMenuItem(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "put",
            Route = "menu/{category}/{sku}")]
        HttpRequestData req,
        string category,
        string sku)
    {
        try
        {
            category = category.Trim();
            sku = sku.Trim();

            UpdateMenuItemRequest? request =
                await JsonSerializer.DeserializeAsync<UpdateMenuItemRequest>(
                    req.Body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (request == null)
            {
                return await Error(
                    req,
                    HttpStatusCode.BadRequest,
                    "Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return await Error(
                    req,
                    HttpStatusCode.BadRequest,
                    "Name is required.");
            }

            if (request.Price < 0)
            {
                return await Error(
                    req,
                    HttpStatusCode.BadRequest,
                    "Price cannot be negative.");
            }

            MenuItemEntity item;

            try
            {
                item = await _tableClient.GetEntityAsync<MenuItemEntity>(
                    category,
                    sku);
            }
            catch (RequestFailedException ex)
                when (ex.Status == 404)
            {
                return await Error(
                    req,
                    HttpStatusCode.NotFound,
                    "Menu item was not found.");
            }

            item.Name = request.Name.Trim();
            item.Description = request.Description?.Trim() ?? "";
            item.Price = request.Price;
            item.IsAvailable = request.IsAvailable;

            await _tableClient.UpdateEntityAsync(
                item,
                item.ETag,
                TableUpdateMode.Replace);

            HttpResponseData response =
                req.CreateResponse(HttpStatusCode.OK);

            await response.WriteAsJsonAsync(item);

            return response;
        }
        catch (JsonException)
        {
            return await Error(
                req,
                HttpStatusCode.BadRequest,
                "Invalid JSON request body.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating menu item.");

            return await Error(
                req,
                HttpStatusCode.InternalServerError,
                ex.Message);
        }
    }


    // ============================================================
    // DELETE
    // DELETE /api/menu/{category}/{sku}
    // ============================================================

    [Function("DeleteMenuItem")]
    public async Task<HttpResponseData> DeleteMenuItem(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "delete",
            Route = "menu/{category}/{sku}")]
        HttpRequestData req,
        string category,
        string sku)
    {
        try
        {
            category = category.Trim();
            sku = sku.Trim();

            try
            {
                await _tableClient.DeleteEntityAsync(
                    category,
                    sku);
            }
            catch (RequestFailedException ex)
                when (ex.Status == 404)
            {
                return await Error(
                    req,
                    HttpStatusCode.NotFound,
                    "Menu item was not found.");
            }

            HttpResponseData response =
                req.CreateResponse(HttpStatusCode.OK);

            await response.WriteAsJsonAsync(
                new
                {
                    message = "Menu item deleted successfully.",
                    category = category,
                    sku = sku
                });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting menu item.");

            return await Error(
                req,
                HttpStatusCode.InternalServerError,
                ex.Message);
        }
    }


    // ============================================================
    // ERROR RESPONSE
    // ============================================================

    private static async Task<HttpResponseData> Error(
        HttpRequestData req,
        HttpStatusCode statusCode,
        string message)
    {
        HttpResponseData response =
            req.CreateResponse(statusCode);

        await response.WriteAsJsonAsync(
            new
            {
                error = message
            });

        return response;
    }
}