using System.Security.Cryptography;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.Auth.Commands;
using Domain.Models;
using Application.Common.Settings;
using Microsoft.Extensions.Options;

namespace Application.UseCases.Auth.Handlers;

internal sealed class ForgotPasswordCommandHandler
    : IRequestHandler<ForgotPasswordCommand, ErrorOr<Unit>>
{
    private readonly IUserRepository               _userRepository;
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IEmailService                 _emailService;
    private readonly TimeProvider                  _timeProvider;
    private readonly string                        _appBaseUrl;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository tokenRepository,
        IEmailService emailService,
        TimeProvider timeProvider,
        IOptions<AppSettings> appSettings)
    {
        _userRepository   = userRepository;
        _tokenRepository  = tokenRepository;
        _emailService     = emailService;
        _timeProvider     = timeProvider;
        _appBaseUrl       = appSettings.Value.AppBaseUrl;
    }

    public async Task<ErrorOr<Unit>> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        // Always return success — prevents email enumeration
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null)
            return Unit.Value;

        string rawToken  = GenerateToken();
        string tokenHash = HashToken(rawToken);

        await _tokenRepository.AddAsync(new PasswordResetToken
        {
            Id        = Guid.NewGuid(),
            UserId    = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = _timeProvider.GetUtcNow().UtcDateTime.AddHours(1),
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
        });

        string fullName  = user.Patient?.FirstName ?? user.Doctor?.FirstName ?? user.Email;
        string resetLink = $"{_appBaseUrl}/reset-password?token={rawToken}";

        await _emailService.SendPasswordResetEmailAsync(user.Email, fullName, resetLink);

        return Unit.Value;
    }

    private static string GenerateToken()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        byte[] bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
