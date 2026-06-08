using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Security;
using Application.Common.Interfaces.Services;
using Application.UseCases.Auth.Commands;
using Application.UseCases.Auth.Common;
using Domain.Enums;
using Domain.Models;

namespace Application.UseCases.Auth.Handlers;

internal sealed class RegisterDoctorCommandHandler
    : IRequestHandler<RegisterDoctorCommand, ErrorOr<RegisterResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICedulaVerificationService _cedulaVerification;

    public RegisterDoctorCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IDateTimeProvider dateTimeProvider,
        ICedulaVerificationService cedulaVerification)
    {
        _userRepository         = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher         = passwordHasher;
        _jwtTokenGenerator      = jwtTokenGenerator;
        _dateTimeProvider       = dateTimeProvider;
        _cedulaVerification     = cedulaVerification;
    }

    public async Task<ErrorOr<RegisterResult>> Handle(
        RegisterDoctorCommand request,
        CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email))
            return AuthErrors.EmailAlreadyExists;

        bool isValid = await _cedulaVerification.VerifyAsync(request.LicenseNumber, cancellationToken);
        if (!isValid)
            return DoctorErrors.InvalidLicense;

        Guid userId = Guid.NewGuid();
        var user = new User
        {
            Id           = userId,
            Email        = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role         = UserRole.Doctor,
            IsActive     = true,
            CreatedAt    = _dateTimeProvider.UtcNow,
            Doctor       = new Domain.Models.Doctor
            {
                Id            = Guid.NewGuid(),
                UserId        = userId,
                FirstName     = request.FirstName,
                LastName      = request.LastName,
                Email         = request.Email,
                LicenseNumber = request.LicenseNumber,
                Speciality    = request.Speciality,
                IsVerified    = true,
                CreatedAt     = _dateTimeProvider.UtcNow,
                IsActive      = true
            }
        };

        await _userRepository.AddAsync(user);

        string fullName     = $"{request.FirstName} {request.LastName}";
        string accessToken  = _jwtTokenGenerator.GenerateToken(user, fullName);
        string refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            Id        = Guid.NewGuid(),
            UserId    = user.Id,
            Token     = refreshToken,
            ExpiresAt = _dateTimeProvider.UtcNow.AddDays(7),
            CreatedAt = _dateTimeProvider.UtcNow,
        });

        return new RegisterResult(
            UserId:       user.Id,
            Email:        user.Email,
            Role:         user.Role.ToString(),
            Token:        accessToken,
            RefreshToken: refreshToken);
    }
}
