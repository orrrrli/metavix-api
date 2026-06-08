namespace Infrastructure.Services;

public sealed class CedulaScraperSettings
{
    public const string SectionName = "CedulaScraper";

    public string BaseUrl { get; init; } = string.Empty;

    // Shared secret sent as X-Internal-Key — scraper rejects any request without it.
    public string InternalKey { get; init; } = string.Empty;
}
