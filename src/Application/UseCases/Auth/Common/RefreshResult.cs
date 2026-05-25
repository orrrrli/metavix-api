namespace Application.UseCases.Auth.Common;

public sealed record RefreshResult(
    string AccessToken,
    string NewRefreshToken,
    DateTime ExpiresAt);
