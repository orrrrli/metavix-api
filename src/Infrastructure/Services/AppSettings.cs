using Application.Common.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public sealed class AppSettings : IAppSettings
{
    private readonly BrevoSettings _brevo;

    public AppSettings(IOptions<BrevoSettings> brevo)
    {
        _brevo = brevo.Value;
    }

    public string AppBaseUrl => _brevo.AppBaseUrl;
}
