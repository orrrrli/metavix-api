using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Security;
using Application.Common.Interfaces.Services;
using Application.UseCases.Auth.Commands;
using Application.UseCases.Auth.Common;
using Domain.Enums;
using Domain.Models;

namespace Application.UseCases.Auth.Handlers;

internal sealed class RefreshCommandHandler
    : IRequestHandler<RefreshCommand, ErrorOr<RefreshResult>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserRepository _userRepository;

    public RefreshCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IDateTimeProvider dateTimeProvider,
        IUserRepository userRepository)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _dateTimeProvider = dateTimeProvider;
        _userRepository = userRepository;
    }

    public async Task<ErrorOr<RefreshResult>> Handle(
        RefreshCommand request,
        CancellationToken cancellationToken)
    {
        RefreshToken? stored = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);

        if (stored is null || stored.IsRevoked || stored.ExpiresAt <= _dateTimeProvider.UtcNow)
            return AuthErrors.InvalidRefreshToken;

        User? user = await _userRepository.GetByIdAsync(stored.UserId);
        if (user is null || !user.IsActive)
            return AuthErrors.InvalidRefreshToken;

        await _refreshTokenRepository.RevokeAsync(stored);

        string fullName = user.Role switch
        {
            UserRole.Doctor  => user.Doctor  is not null ? $"{user.Doctor.FirstName} {user.Doctor.LastName}"   : user.Email,
            UserRole.Patient => user.Patient is not null ? $"{user.Patient.FirstName} {user.Patient.LastName}" : user.Email,
            _                => user.Email
        };

        string newAccessToken   = _jwtTokenGenerator.GenerateToken(user, fullName);
        string newRefreshToken  = _jwtTokenGenerator.GenerateRefreshToken();

        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            Id        = Guid.NewGuid(),
            UserId    = user.Id,
            Token     = newRefreshToken,
            ExpiresAt = _dateTimeProvider.UtcNow.AddDays(7),
            CreatedAt = _dateTimeProvider.UtcNow,
        });

        return new RefreshResult(
            AccessToken:     newAccessToken,
            NewRefreshToken: newRefreshToken,
            ExpiresAt:       _dateTimeProvider.UtcNow.AddMinutes(15));
    }
}
