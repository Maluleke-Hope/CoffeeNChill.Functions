using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.ApiGateway.Models;

public class RegisterRequestDto
{
    [Required, StringLength(120)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, MinLength(12)]
    public string Password { get; set; } = "";
}