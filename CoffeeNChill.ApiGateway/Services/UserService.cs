using Azure.Data.Tables;
using CoffeeNChill.ApiGateway.Models;

namespace CoffeeNChill.ApiGateway.Services;

public class UserService : IUserService
{
    private readonly TableClient _usersTable;

    public UserService(TableServiceClient tableServiceClient)
    {
        _usersTable = tableServiceClient.GetTableClient("Users");
        _usersTable.CreateIfNotExists();
    }

    public async Task<bool> RegisterAsync(RegisterRequestDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var existingUser = await _usersTable.GetEntityIfExistsAsync<UserEntity>("Customer", email);
        if (existingUser.HasValue) return false; // Email already in use

        var user = new UserEntity
        {
            PartitionKey = "Customer", // Default role
            RowKey = email,
            Email = email,
            FullName = request.FullName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        await _usersTable.AddEntityAsync(user);
        return true;
    }

    public async Task<UserEntity?> AuthenticateAsync(LoginRequestDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _usersTable.GetEntityIfExistsAsync<UserEntity>("Customer", email);

        if (user.HasValue && BCrypt.Net.BCrypt.Verify(request.Password, user.Value.PasswordHash))
        {
            return user.Value;
        }
        return null;
    }
}
