namespace Contracts.Auth;

public record AuthResponse(
    DateTime ExpiresAt,
    string Email,
    string Role,
    string FullName);
