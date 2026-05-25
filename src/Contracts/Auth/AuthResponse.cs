namespace Contracts.Auth;

public record AuthResponse(
    Guid UserId,
    DateTime ExpiresAt,
    string Email,
    string Role,
    string FullName);
