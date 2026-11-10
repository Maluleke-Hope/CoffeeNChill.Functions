using Microsoft.AspNetCore.Mvc;
using CoffeeNChill.ApiGateway.Models;
using CoffeeNChill.ApiGateway.Services;

namespace CoffeeNChill.ApiGateway.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserRegistrationService _registration;
    private readonly IUserLoginService _login;

    public AuthController(IUserRegistrationService registration, IUserLoginService login)
    {
        _registration = registration;
        _login = login;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequestDto dto, CancellationToken ct)
    {
        var result = await _registration.RegisterAsync(dto, ct);
        if (!result.Created) return Conflict(new { result.Error });
        return StatusCode(201, new { result.Email });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto dto, CancellationToken ct)
    {
        var response = await _login.LoginAsync(dto, ct);
        if (response is null) return Unauthorized(new { error = "Invalid credentials" });
        return Ok(response);
    }
}