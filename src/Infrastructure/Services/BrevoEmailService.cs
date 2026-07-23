using System.Net.Http.Json;
using System.Text.Json;
using Application.Common.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public sealed class BrevoEmailService : IEmailService
{
    private readonly HttpClient    _http;
    private readonly BrevoSettings _settings;

    public BrevoEmailService(HttpClient http, IOptions<BrevoSettings> settings)
    {
        _http     = http;
        _settings = settings.Value;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink)
    {
        var payload = new
        {
            sender  = new { name = _settings.SenderName, email = _settings.SenderEmail },
            to      = new[] { new { email = toEmail, name = toName } },
            subject = "Restablece tu contraseña — Metavix",
            htmlContent = BuildHtml(toName, resetLink),
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        request.Headers.Add("api-key", _settings.ApiKey);
        request.Content = JsonContent.Create(payload, options: new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Brevo {(int)response.StatusCode}: {body}");
        }
    }

    private static string BuildHtml(string name, string resetLink) => $"""
        <div style="font-family:sans-serif;max-width:560px;margin:0 auto;padding:32px">
          <h2 style="color:#1A2D2A">Restablecer contraseña</h2>
          <p>Hola {name},</p>
          <p>Recibimos una solicitud para restablecer la contraseña de tu cuenta en Metavix.</p>
          <p style="margin:32px 0">
            <a href="{resetLink}"
               style="background:#14b8a6;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;font-weight:600">
              Restablecer contraseña
            </a>
          </p>
          <p style="color:#666;font-size:14px">Este enlace expira en 1 hora. Si no solicitaste este cambio, ignora este correo.</p>
          <p style="color:#999;font-size:13px">Si el botón no funciona, copia y pega este enlace en tu navegador:<br/><a href="{resetLink}" style="color:#14b8a6;word-break:break-all">{resetLink}</a></p>
          <hr style="border:none;border-top:1px solid #eee;margin:32px 0"/>
          <p style="color:#999;font-size:12px">Metavix — metavix.com.mx</p>
        </div>
        """;
}
