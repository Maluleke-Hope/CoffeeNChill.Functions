using CoffeeNChill.ApiGateway.Models;

namespace CoffeeNChill.ApiGateway.Services;

public interface ITokenService
{
    AuthResponseDto GenerateToken(UserEntity user);
}
