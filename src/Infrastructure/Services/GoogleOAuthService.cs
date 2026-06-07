using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Application.Common.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public sealed class GoogleOAuthService : IGoogleOAuthService
{
    private const string StatePrefix = "google_oauth_state_";

    private readonly HttpClient            _http;
    private readonly GoogleOAuthSettings   _settings;
    private readonly IMemoryCache          _cache;

    public string FrontendUrl => _settings.FrontendUrl;

    public GoogleOAuthService(
        HttpClient http,
        IOptions<GoogleOAuthSettings> settings,
        IMemoryCache cache)
    {
        _http     = http;
        _settings = settings.Value;
        _cache    = cache;
    }

    public string BuildAuthorizationUrl(string role, out string state)
    {
        state = GenerateState();
        _cache.Set($"{StatePrefix}{state}", role, TimeSpan.FromMinutes(10));

        return "https://accounts.google.com/o/oauth2/v2/auth" +
            $"?client_id={Uri.EscapeDataString(_settings.ClientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(_settings.RedirectUri)}" +
            $"&response_type=code" +
            $"&scope=openid%20email%20profile" +
            $"&state={state}" +
            $"&access_type=offline" +
            $"&prompt=select_account";
    }

    public bool ValidateAndConsumeState(string state, out string role)
    {
        if (_cache.TryGetValue($"{StatePrefix}{state}", out string? cached) && cached is not null)
        {
            role = cached;
            _cache.Remove($"{StatePrefix}{state}");
            return true;
        }

        role = string.Empty;
        return false;
    }

    public async Task<GoogleUserInfo> GetUserInfoAsync(string code)
    {
        string accessToken = await ExchangeCodeAsync(code);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://www.googleapis.com/oauth2/v3/userinfo");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        HttpResponseMessage response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        GoogleUserInfoResponse info = (await response.Content
            .ReadFromJsonAsync<GoogleUserInfoResponse>())!;

        string[] nameParts  = (info.Name ?? string.Empty).Split(' ', 2);
        string   firstName  = info.GivenName  ?? (nameParts.Length > 0 ? nameParts[0] : string.Empty);
        string   lastName   = info.FamilyName ?? (nameParts.Length > 1 ? nameParts[1] : string.Empty);

        return new GoogleUserInfo(
            Email:     info.Email,
            FirstName: firstName,
            LastName:  lastName,
            GoogleId:  info.Sub);
    }

    private async Task<string> ExchangeCodeAsync(string code)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"]    = "authorization_code",
            ["code"]          = code,
            ["client_id"]     = _settings.ClientId,
            ["client_secret"] = _settings.ClientSecret,
            ["redirect_uri"]  = _settings.RedirectUri,
        });

        HttpResponseMessage response = await _http.PostAsync(
            "https://oauth2.googleapis.com/token", content);
        response.EnsureSuccessStatusCode();

        GoogleTokenResponse tokens = (await response.Content
            .ReadFromJsonAsync<GoogleTokenResponse>())!;

        return tokens.AccessToken;
    }

    private static string GenerateState()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(24);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').Replace("=", string.Empty);
    }

    // ── Private deserialization models ─────────────────────────────────────

    private sealed class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;
    }

    private sealed class GoogleUserInfoResponse
    {
        [JsonPropertyName("sub")]
        public string Sub { get; init; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; init; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("given_name")]
        public string? GivenName { get; init; }

        [JsonPropertyName("family_name")]
        public string? FamilyName { get; init; }
    }
}
