namespace ST10467898CLDV6212POE.DTOs;

public record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string Role // "Admin", "Staff", or "Customer"
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResponse(
    string Token,
    string Email,
    string Role,
    string FullName
);