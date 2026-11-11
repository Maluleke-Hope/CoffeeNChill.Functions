using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CoffeeNChill.ApiGateway.Models;
using Microsoft.IdentityModel.Tokens;

namespace CoffeeNChill.ApiGateway.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config) => _config = config;

    public AuthResponseDto GenerateToken(UserEntity user)
    {
        var secret = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key missing");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.RowKey),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Role, user.PartitionKey), // CRITICAL FOR RBAC
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: creds
        );

        return new AuthResponseDto(
            new JwtSecurityTokenHandler().WriteToken(token),
            token.ValidTo
        );
    }
}
