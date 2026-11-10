using Azure;
using Azure.Data.Tables;
using CoffeeNChill.ApiGateway.Models;
using Microsoft.AspNetCore.Identity;

namespace CoffeeNChill.ApiGateway.Services;

public interface IUserRegistrationService
{
    Task<RegistrationResult> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
}

public record RegistrationResult(bool Created, string? Error, string? Email);

public class UserRegistrationService : IUserRegistrationService
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly IPasswordHasher<UserEntity> _passwordHasher;

    public UserRegistrationService(TableServiceClient tableServiceClient, IPasswordHasher<UserEntity> passwordHasher)
    {
        _tableServiceClient = tableServiceClient;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegistrationResult> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var fullName = request.FullName.Trim();

        var usersTable = _tableServiceClient.GetTableClient("Users");
        var indexTable = _tableServiceClient.GetTableClient("UserEmailIndex");

        await usersTable.CreateIfNotExistsAsync(cancellationToken);
        await indexTable.CreateIfNotExistsAsync(cancellationToken);

        var indexEntity = new UserEmailIndexEntity
        {
            RowKey = email,
            UserPartitionKey = "Customer",
            UserRowKey = email
        };

        try
        {
            await indexTable.AddEntityAsync(indexEntity, cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            return new(false, "Email already registered.", null);
        }

        var user = new UserEntity
        {
            PartitionKey = "Customer",
            RowKey = email,
            Email = email,
            FullName = fullName,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        try
        {
            await usersTable.AddEntityAsync(user, cancellationToken);
        }
        catch
        {
            await indexTable.DeleteEntityAsync("EMAIL", email, ETag.All, cancellationToken);
            throw;
        }

        return new(true, null, email);
    }
}