using System.Security.Cryptography;
using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Security;
using Application.UseCases.Auth.Commands;

namespace Application.UseCases.Auth.Handlers;

internal sealed class ResetPasswordCommandHandler
    : IRequestHandler<ResetPasswordCommand, ErrorOr<Unit>>
{
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IUserRepository               _userRepository;
    private readonly IPasswordHasher               _passwordHasher;
    private readonly TimeProvider             _timeProvider;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenRepository tokenRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        TimeProvider timeProvider)
    {
        _tokenRepository  = tokenRepository;
        _userRepository   = userRepository;
        _passwordHasher   = passwordHasher;
        _timeProvider = timeProvider;
    }

    public async Task<ErrorOr<Unit>> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        string tokenHash = HashToken(request.Token);

        var resetToken = await _tokenRepository.GetByTokenHashAsync(tokenHash);

        if (resetToken is null || resetToken.UsedAt is not null)
            return AuthErrors.InvalidOrExpiredResetToken;

        if (resetToken.ExpiresAt < _timeProvider.GetUtcNow().UtcDateTime)
            return AuthErrors.InvalidOrExpiredResetToken;

        var user = await _userRepository.GetByIdAsync(resetToken.UserId);
        if (user is null)
            return AuthErrors.InvalidOrExpiredResetToken;

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt    = _timeProvider.GetUtcNow().UtcDateTime;

        await _userRepository.UpdateAsync(user);
        await _tokenRepository.MarkAsUsedAsync(resetToken);

        return Unit.Value;
    }

    private static string HashToken(string token)
    {
        byte[] bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
