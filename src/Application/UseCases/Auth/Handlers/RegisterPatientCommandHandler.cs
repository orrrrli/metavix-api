using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Security;
using Application.Common.Interfaces.Services;
using Application.UseCases.Auth.Commands;
using Application.UseCases.Auth.Common;
using Domain.Enums;
using Domain.Models;

namespace Application.UseCases.Auth.Handlers;

internal sealed class RegisterPatientCommandHandler
    : IRequestHandler<RegisterPatientCommand, ErrorOr<RegisterResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RegisterPatientCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IDateTimeProvider dateTimeProvider)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ErrorOr<RegisterResult>> Handle(
        RegisterPatientCommand request,
        CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email))
            return AuthErrors.EmailAlreadyExists;

        Guid userId = Guid.NewGuid();
        var user = new User
        {
            Id           = userId,
            Email        = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role         = UserRole.Patient,
            IsActive     = true,
            CreatedAt    = _dateTimeProvider.UtcNow,
            Patient      = new Domain.Models.Patient
            {
                Id             = Guid.NewGuid(),
                UserId         = userId,
                FirstName      = request.FirstName,
                LastName       = request.LastName,
                Email          = request.Email,
                CreatedAt      = _dateTimeProvider.UtcNow,
                IsActive       = true,
                PrimaryDoctorId = null
            }
        };

        await _userRepository.AddAsync(user);

        string fullName    = $"{request.FirstName} {request.LastName}";
        string accessToken = _jwtTokenGenerator.GenerateToken(user, fullName);
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
