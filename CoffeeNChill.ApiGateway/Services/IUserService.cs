using CoffeeNChill.ApiGateway.Models;

namespace CoffeeNChill.ApiGateway.Services;

public interface IUserService
{
    Task<bool> RegisterAsync(RegisterRequestDto request);
    Task<UserEntity?> AuthenticateAsync(LoginRequestDto request);
}
