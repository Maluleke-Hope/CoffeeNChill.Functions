using System.Net;
using Azure.Data.Tables;
using ST10467898CLDV6212POE.Models;
using ST10467898CLDV6212POE.DTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ST10467898CLDV6212POE.Functions
{
    public class MenuFunctions
    {
        private readonly TableClient _tableClient;
        private readonly ILogger<MenuFunctions> _logger;

        public MenuFunctions(IConfiguration config, ILogger<MenuFunctions> logger)
        {
            _logger = logger;
            string conn = config["AzureWebJobsStorage"] ?? string.Empty;
            var serviceClient = new TableServiceClient(conn);
            _tableClient = serviceClient.GetTableClient("MenuItems");
            _tableClient.CreateIfNotExists();
        }

        [Function("CreateMenuItem")]
        public async Task<HttpResponseData> Create([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "menu")] HttpRequestData req)
        {
            string body = await new StreamReader(req.Body).ReadToEndAsync();
            var dto = JsonConvert.DeserializeObject<CreateMenuItemRequest>(body);

            var entity = new MenuItemEntity
            {
                RowKey = Guid.NewGuid().ToString(),
                PartitionKey = dto?.Category ?? "General",
                Name = dto?.Name ?? "Item",
                Price = dto?.Price ?? 0,
                IsAvailable = dto?.IsAvailable ?? true
            };

            await _tableClient.AddEntityAsync(entity);
            var res = req.CreateResponse(HttpStatusCode.Created);
            await res.WriteAsJsonAsync(entity);
            return res;
        }

        [Function("GetAllMenuItems")]
        public async Task<HttpResponseData> GetAll([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu")] HttpRequestData req)
        {
            var items = new List<MenuItemEntity>();
            await foreach (var item in _tableClient.QueryAsync<MenuItemEntity>())
            {
                items.Add(item);
            }
            var res = req.CreateResponse(HttpStatusCode.OK);
            await res.WriteAsJsonAsync(items);
            return res;
        }
    }
}