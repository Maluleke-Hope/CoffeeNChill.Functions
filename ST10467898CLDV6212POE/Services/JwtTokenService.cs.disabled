using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using CoffeeNChill.ApiGateway.Models;


namespace CoffeeNChill.ApiGateway.Services;

public interface IJwtTokenService
{
    LoginResponseDto GenerateToken(UserEntity user);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config) => _config = config;

    public LoginResponseDto GenerateToken(UserEntity user)
    {
        var secret = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT signing key missing.");
        if (Encoding.UTF8.GetByteCount(secret) < 32)
            throw new InvalidOperationException("JWT signing key must be >= 32 bytes.");

        var issuer = _config["Jwt:Issuer"]!;
        var audience = _config["Jwt:Audience"]!;
        var expiry = DateTime.UtcNow.AddMinutes(60);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.RowKey),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Role, user.PartitionKey)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(issuer, audience, claims, expires: expiry, signingCredentials: credentials);
        return new LoginResponseDto(new JwtSecurityTokenHandler().WriteToken(token), expiry);
    }
}