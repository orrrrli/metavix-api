using System.Security.Cryptography;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.Auth.Commands;
using Domain.Models;

namespace Application.UseCases.Auth.Handlers;

internal sealed class ForgotPasswordCommandHandler
    : IRequestHandler<ForgotPasswordCommand, ErrorOr<Unit>>
{
    private readonly IUserRepository               _userRepository;
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IEmailService                 _emailService;
    private readonly IDateTimeProvider             _dateTimeProvider;
    private readonly IAppSettings                  _appSettings;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository tokenRepository,
        IEmailService emailService,
        IDateTimeProvider dateTimeProvider,
        IAppSettings appSettings)
    {
        _userRepository   = userRepository;
        _tokenRepository  = tokenRepository;
        _emailService     = emailService;
        _dateTimeProvider = dateTimeProvider;
        _appSettings      = appSettings;
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
            ExpiresAt = _dateTimeProvider.UtcNow.AddHours(1),
            CreatedAt = _dateTimeProvider.UtcNow,
        });

        string fullName  = user.Patient?.FirstName ?? user.Doctor?.FirstName ?? user.Email;
        string resetLink = $"{_appSettings.AppBaseUrl}/reset-password?token={rawToken}";

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
