using System.Security.Cryptography;
using System.Text.Json;
using Application.Common.Interfaces.Persistence;
using Application.Common.Outbox;
using Application.UseCases.Auth.Commands;
using Application.UseCases.Auth.Outbox;
using Domain.Models;
using Application.Common.Settings;
using Microsoft.Extensions.Options;

namespace Application.UseCases.Auth.Handlers;

internal sealed class ForgotPasswordCommandHandler
    : IRequestHandler<ForgotPasswordCommand, ErrorOr<Unit>>
{
    private readonly IUserRepository               _userRepository;
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IOutboxRepository             _outboxRepository;
    private readonly TimeProvider                  _timeProvider;
    private readonly string                        _appBaseUrl;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository tokenRepository,
        IOutboxRepository outboxRepository,
        TimeProvider timeProvider,
        IOptions<AppSettings> appSettings)
    {
        _userRepository   = userRepository;
        _tokenRepository  = tokenRepository;
        _outboxRepository = outboxRepository;
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

        var payload = new PasswordResetEmailPayload(user.Email, fullName, resetLink);

        // Tracked on the same DbContext as the token above — PersistenceBehavior's
        // single trailing SaveChangesAsync commits both rows atomically. The email
        // is only ever sent once OutboxProcessorService observes the committed row.
        await _outboxRepository.AddAsync(new OutboxMessage
        {
            Id        = Guid.NewGuid(),
            Type      = OutboxMessageTypes.PasswordResetEmail,
            Payload   = JsonSerializer.Serialize(payload),
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
        }, cancellationToken);

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
