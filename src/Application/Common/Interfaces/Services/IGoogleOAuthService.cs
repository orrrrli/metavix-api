namespace Application.Common.Interfaces.Services;

public sealed record GoogleUserInfo(
    string Email,
    string FirstName,
    string LastName,
    string GoogleId);

public interface IGoogleOAuthService
{
    string FrontendUrl { get; }
    string BuildAuthorizationUrl(string role, out string state);
    bool ValidateAndConsumeState(string state, out string role);
    Task<GoogleUserInfo> GetUserInfoAsync(string code);
}
