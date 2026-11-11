using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public MenuController(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    // GET: api/menu (Anyone can view the menu)
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetMenu()
    {
        var functionUrl = _config["FunctionsBaseUrl"] ?? "http://localhost:7071";
        var response = await _httpClient.GetAsync($"{functionUrl}/api/menu");

        if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        return Content(content, "application/json");
    }

    // POST: api/menu (Only Staff and Admin can create menu items)
    [HttpPost]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> CreateMenuItem([FromBody] object menuItemDto)
    {
        var functionUrl = _config["FunctionsBaseUrl"] ?? "http://localhost:7071";
        var response = await _httpClient.PostAsJsonAsync($"{functionUrl}/api/menu", menuItemDto);

        if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        return Content(content, "application/json");
    }

    // PUT: api/menu/{category}/{sku} (Only Staff and Admin)
    [HttpPut("{category}/{sku}")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> UpdateMenuItem(string category, string sku, [FromBody] object updateDto)
    {
        var functionUrl = _config["FunctionsBaseUrl"] ?? "http://localhost:7071";
        var response = await _httpClient.PutAsJsonAsync($"{functionUrl}/api/menu/{category}/{sku}", updateDto);

        if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode);

        return Ok(new { message = "Menu item updated successfully" });
    }
}