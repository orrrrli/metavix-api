namespace Application.UseCases.Auth.Login;

public sealed record LoginResult(
    string AccessToken,
    DateTime ExpiresAt,
    string Email,
    string Role,
    string FullName);
