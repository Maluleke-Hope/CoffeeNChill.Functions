using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ST10467898CLDV6212POE.DTOs;
using ST10467898CLDV6212POE.Models;

namespace ST10467898CLDV6212POE.Functions;

public class AuthFunctions
{
    private readonly TableClient _usersTableClient;
    private readonly ILogger<AuthFunctions> _logger;
    private readonly PasswordHasher<object> _passwordHasher = new();
    private const string TableName = "Users";

    public AuthFunctions(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<AuthFunctions>();
        string? connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

        TableServiceClient tableServiceClient = new TableServiceClient(connectionString);
        _usersTableClient = tableServiceClient.GetTableClient(TableName);

        try { _usersTableClient.CreateIfNotExists(); } catch { }
    }

    [Function("Register")]
    public async Task<HttpResponseData> Register(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/register")] HttpRequestData req)
    {
        try
        {
            var body = await JsonSerializer.DeserializeAsync<RegisterRequest>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (body == null || string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
            {
                return await ErrorResponse(req, HttpStatusCode.BadRequest, "Email and Password are required.");
            }

            string role = string.IsNullOrWhiteSpace(body.Role) ? "Customer" : body.Role.Trim();
            string email = body.Email.Trim().ToLowerInvariant();

            // Check if user exists
            try
            {
                var existing = await _usersTableClient.GetEntityAsync<UserEntity>(role, email);
                if (existing.Value != null)
                    return await ErrorResponse(req, HttpStatusCode.Conflict, "User already exists.");
            }
            catch { /* User doesn't exist yet, proceed */ }

            var user = new UserEntity
            {
                PartitionKey = role,
                RowKey = email,
                FullName = body.FullName?.Trim() ?? "",
                PasswordHash = _passwordHasher.HashPassword(null!, body.Password),
                CreatedDate = DateTime.UtcNow
            };

            await _usersTableClient.AddEntityAsync(user);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(new { message = "User registered successfully." });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration.");
            return await ErrorResponse(req, HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [Function("Login")]
    public async Task<HttpResponseData> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequestData req)
    {
        try
        {
            var body = await JsonSerializer.DeserializeAsync<LoginRequest>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (body == null || string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
            {
                return await ErrorResponse(req, HttpStatusCode.BadRequest, "Email and Password are required.");
            }

            string email = body.Email.Trim().ToLowerInvariant();
            UserEntity? foundUser = null;

            // Search across possible roles (Admin, Staff, Customer)
            string[] roles = { "Admin", "Staff", "Customer" };
            foreach (var r in roles)
            {
                try
                {
                    var res = await _usersTableClient.GetEntityAsync<UserEntity>(r, email);
                    if (res.Value != null)
                    {
                        foundUser = res.Value;
                        break;
                    }
                }
                catch { }
            }

            if (foundUser == null)
            {
                return await ErrorResponse(req, HttpStatusCode.Unauthorized, "Invalid email or password.");
            }

            var verifyResult = _passwordHasher.VerifyHashedPassword(null!, foundUser.PasswordHash, body.Password);
            if (verifyResult == PasswordVerificationResult.Failed)
            {
                return await ErrorResponse(req, HttpStatusCode.Unauthorized, "Invalid email or password.");
            }

            // Generate JWT Token
            string token = GenerateJwtToken(foundUser);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new AuthResponse(token, foundUser.RowKey, foundUser.PartitionKey, foundUser.FullName));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login.");
            return await ErrorResponse(req, HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    private string GenerateJwtToken(UserEntity user)
    {
        string keyStr = Environment.GetEnvironmentVariable("Jwt__Key") ?? "SuperSecretKeyWithAtLeast32BytesLengthHereForCoffeeNChillGateway";
        var key = Encoding.UTF8.GetBytes(keyStr);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.RowKey),
            new(ClaimTypes.Email, user.RowKey),
            new(ClaimTypes.Role, user.PartitionKey),
            new("FullName", user.FullName)
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(8),
            Issuer = Environment.GetEnvironmentVariable("Jwt__Issuer") ?? "CoffeeNChillAPI",
            Audience = Environment.GetEnvironmentVariable("Jwt__Audience") ?? "CoffeeNChillClients",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }

    private static async Task<HttpResponseData> ErrorResponse(HttpRequestData req, HttpStatusCode code, string message)
    {
        var res = req.CreateResponse(code);
        await res.WriteAsJsonAsync(new { error = message });
        return res;
    }
}