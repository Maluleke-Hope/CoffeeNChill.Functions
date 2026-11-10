using Azure;
using Azure.Data.Tables;
using CoffeeNChill.ApiGateway.Models;
using Microsoft.AspNetCore.Identity;

namespace CoffeeNChill.ApiGateway.Services;

public interface IUserLoginService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}

public class UserLoginService : IUserLoginService
{
    private readonly TableClient _users;
    private readonly TableClient _emailIndex;
    private readonly IPasswordHasher<UserEntity> _hasher;
    private readonly IJwtTokenService _tokens;

    public UserLoginService(TableServiceClient tables, IPasswordHasher<UserEntity> hasher, IJwtTokenService tokens)
    {
        _users = tables.GetTableClient("Users");
        _emailIndex = tables.GetTableClient("UserEmailIndex");
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var indexResponse = await _emailIndex.GetEntityIfExistsAsync<UserEmailIndexEntity>("EMAIL", email, cancellationToken: cancellationToken);
        if (!indexResponse.HasValue) return null;

        var index = indexResponse.Value!;
        var userResponse = await _users.GetEntityIfExistsAsync<UserEntity>(index.UserPartitionKey, index.UserRowKey, cancellationToken: cancellationToken);
        if (!userResponse.HasValue) return null;

        var user = userResponse.Value!;
        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed) return null;

        return _tokens.GenerateToken(user);
    }
}