namespace Contracts.Auth;

public record RegisterResponse(
    Guid UserId,
    string Email,
    string Role);
