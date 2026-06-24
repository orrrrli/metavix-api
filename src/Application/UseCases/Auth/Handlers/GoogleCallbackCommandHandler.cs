using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Security;
using Application.Common.Interfaces.Services;
using Application.UseCases.Auth.Commands;
using Application.UseCases.Auth.Common;
using Domain.Enums;
using Domain.Models;

namespace Application.UseCases.Auth.Handlers;

internal sealed class GoogleCallbackCommandHandler
    : IRequestHandler<GoogleCallbackCommand, ErrorOr<LoginResult>>
{
    private readonly IGoogleOAuthService          _googleOAuthService;
    private readonly IUserRepository              _userRepository;
    private readonly IRefreshTokenRepository      _refreshTokenRepository;
    private readonly IJwtTokenGenerator           _jwtTokenGenerator;
    private readonly TimeProvider            _timeProvider;

    public GoogleCallbackCommandHandler(
        IGoogleOAuthService googleOAuthService,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        TimeProvider timeProvider)
    {
        _googleOAuthService     = googleOAuthService;
        _userRepository         = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenGenerator      = jwtTokenGenerator;
        _timeProvider       = timeProvider;
    }

    public async Task<ErrorOr<LoginResult>> Handle(
        GoogleCallbackCommand request,
        CancellationToken cancellationToken)
    {
        if (!_googleOAuthService.ValidateAndConsumeState(request.State, out string role))
            return AuthErrors.GoogleOAuthFailed;

        GoogleUserInfo googleUser;
        try
        {
            googleUser = await _googleOAuthService.GetUserInfoAsync(request.Code);
        }
        catch
        {
            return AuthErrors.GoogleOAuthFailed;
        }

        User? user = await _userRepository.GetByEmailAsync(googleUser.Email);

        if (user is null)
        {
            Guid     userId   = Guid.NewGuid();
            UserRole userRole = role == "doctor" ? UserRole.Doctor : UserRole.Patient;

            user = new User
            {
                Id           = userId,
                Email        = googleUser.Email,
                PasswordHash = string.Empty,
                Role         = userRole,
                IsActive     = true,
                CreatedAt    = _timeProvider.GetUtcNow().UtcDateTime,
            };

            if (userRole == UserRole.Patient)
            {
                user.Patient = new Domain.Models.Patient
                {
                    Id        = Guid.NewGuid(),
                    UserId    = userId,
                    FirstName = googleUser.FirstName,
                    LastName  = googleUser.LastName,
                    Email     = googleUser.Email,
                    CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
                    IsActive  = true,
                };
            }
            else
            {
                user.Doctor = new Domain.Models.Doctor
                {
                    Id            = Guid.NewGuid(),
                    UserId        = userId,
                    FirstName        = googleUser.FirstName,
                    PaternalLastName = googleUser.LastName,
                    MaternalLastName = string.Empty,
                    Email         = googleUser.Email,
                    LicenseNumber = string.Empty,
                    Speciality    = string.Empty,
                    CreatedAt     = _timeProvider.GetUtcNow().UtcDateTime,
                    IsActive      = true,
                };
            }

            await _userRepository.AddAsync(user);
        }

        string fullName = user.Role switch
        {
            UserRole.Doctor  => user.Doctor  is not null
                ? $"{user.Doctor.FirstName} {user.Doctor.PaternalLastName}"
                : user.Email,
            UserRole.Patient => user.Patient is not null
                ? $"{user.Patient.FirstName} {user.Patient.LastName}"
                : user.Email,
            _ => user.Email,
        };

        string accessToken  = _jwtTokenGenerator.GenerateToken(user, fullName);
        string refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            Id        = Guid.NewGuid(),
            UserId    = user.Id,
            Token     = refreshToken,
            ExpiresAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
        });

        return new LoginResult(
            UserId:       user.Id,
            PatientId:    user.Patient?.Id,
            DoctorId:     user.Doctor?.Id,
            AccessToken:  accessToken,
            RefreshToken: refreshToken,
            ExpiresAt:    _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(15),
            Email:        user.Email,
            Role:         user.Role.ToString(),
            FullName:     fullName);
    }
}
