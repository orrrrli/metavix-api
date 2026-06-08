using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Security;
using Application.Common.Interfaces.Services;
using Application.UseCases.Auth.Commands;
using Application.UseCases.Auth.Common;
using Domain.Enums;
using Domain.Models;

namespace Application.UseCases.Auth.Handlers;

internal sealed class LoginCommandHandler
    : IRequestHandler<LoginCommand, ErrorOr<LoginResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILoginAttemptTracker _attemptTracker;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IDateTimeProvider dateTimeProvider,
        ILoginAttemptTracker attemptTracker)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _dateTimeProvider = dateTimeProvider;
        _attemptTracker = attemptTracker;
    }

    public async Task<ErrorOr<LoginResult>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        if (_attemptTracker.IsBlocked(request.Email))
            return AuthErrors.TooManyFailedAttempts;

        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user is null)
        {
            _attemptTracker.RegisterFailure(request.Email);
            return AuthErrors.InvalidCredentials;
        }

        if (string.IsNullOrEmpty(user.PasswordHash))
            return AuthErrors.GoogleAccountOnly;

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _attemptTracker.RegisterFailure(request.Email);
            return AuthErrors.InvalidCredentials;
        }

        if (!user.IsActive)
            return AuthErrors.AccountInactive;

        _attemptTracker.ResetAttempts(request.Email);

        string fullName = user.Role switch
        {
            UserRole.Doctor  => user.Doctor  is not null ? $"{user.Doctor.FirstName} {user.Doctor.PaternalLastName}"   : user.Email,
            UserRole.Patient => user.Patient is not null ? $"{user.Patient.FirstName} {user.Patient.LastName}" : user.Email,
            _                => user.Email
        };

        string accessToken   = _jwtTokenGenerator.GenerateToken(user, fullName);
        string refreshToken  = _jwtTokenGenerator.GenerateRefreshToken();

        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            Id        = Guid.NewGuid(),
            UserId    = user.Id,
            Token     = refreshToken,
            ExpiresAt = _dateTimeProvider.UtcNow.AddDays(7),
            CreatedAt = _dateTimeProvider.UtcNow,
        });

        return new LoginResult(
            UserId:       user.Id,
            PatientId:    user.Patient?.Id,
            DoctorId:     user.Doctor?.Id,
            AccessToken:  accessToken,
            RefreshToken: refreshToken,
            ExpiresAt:    _dateTimeProvider.UtcNow.AddMinutes(15),
            Email:        user.Email,
            Role:         user.Role.ToString(),
            FullName:     fullName);
    }
}
