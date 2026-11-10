using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.ApiGateway.Models;

public class LoginRequestDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}

public record LoginResponseDto(string AccessToken, DateTime ExpiresAtUtc, string TokenType = "Bearer");