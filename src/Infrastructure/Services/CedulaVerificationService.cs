using System.Net.Http.Json;
using System.Text.Json;
using Application.Common.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

internal sealed class CedulaVerificationService : ICedulaVerificationService
{
    private readonly HttpClient _http;
    private readonly CedulaScraperSettings _settings;
    private readonly ILogger<CedulaVerificationService> _logger;

    public CedulaVerificationService(
        HttpClient http,
        IOptions<CedulaScraperSettings> settings,
        ILogger<CedulaVerificationService> logger)
    {
        _http     = http;
        _settings = settings.Value;
        _logger   = logger;
    }

    public async Task<bool> VerifyAsync(string licenseNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.InternalKey))
        {
            _logger.LogWarning("CedulaScraper InternalKey not configured — skipping cedula verification.");
            return false;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/verify")
            {
                Content = JsonContent.Create(new { licenseNumber })
            };
            request.Headers.Add("X-Internal-Key", _settings.InternalKey);

            using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "CedulaScraper returned {StatusCode} for license {LicenseNumber}",
                    (int)response.StatusCode, licenseNumber);
                return false;
            }

            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            using JsonDocument doc = JsonDocument.Parse(body);

            return doc.RootElement.TryGetProperty("isValid", out JsonElement isValid)
                && isValid.GetBoolean();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling CedulaScraper for license {LicenseNumber}", licenseNumber);
            return false;
        }
    }
}
