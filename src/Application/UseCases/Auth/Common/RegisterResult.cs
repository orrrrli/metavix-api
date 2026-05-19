namespace Application.UseCases.Auth.Common;

public sealed record RegisterResult(
    Guid UserId,
    string Email,
    string Role,
    string Token);
