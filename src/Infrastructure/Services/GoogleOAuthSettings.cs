namespace Infrastructure.Services;

public sealed class GoogleOAuthSettings
{
    public const string SectionName = "Google";

    public string ClientId     { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string RedirectUri  { get; init; } = string.Empty;
    public string FrontendUrl  { get; init; } = string.Empty;
}
