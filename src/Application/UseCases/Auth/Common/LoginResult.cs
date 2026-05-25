namespace Application.UseCases.Auth.Common;

public sealed record LoginResult(
    Guid UserId,
    string AccessToken,
    DateTime ExpiresAt,
    string Email,
    string Role,
    string FullName);
