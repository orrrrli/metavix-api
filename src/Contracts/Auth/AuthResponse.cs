namespace Contracts.Auth;

public record AuthResponse(
    string AccessToken,
    DateTime ExpiresAt,
    string Email,
    string Role,
    string FullName);
