using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Security;
using Application.Common.Interfaces.Services;
using Application.UseCases.Auth.Commands;
using Application.UseCases.Auth.Common;
using Domain.Enums;
using Domain.Models;

namespace Application.UseCases.Auth.Handlers;

internal sealed class RegisterCommandHandler
    : IRequestHandler<RegisterCommand, ErrorOr<RegisterResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IDateTimeProvider dateTimeProvider)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ErrorOr<RegisterResult>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Check if email already exists
        if (await _userRepository.ExistsByEmailAsync(request.Email))
        {
            return AuthErrors.EmailAlreadyExists;
        }

        // 2. Create User entity
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = request.Role,
            IsActive = true,
            CreatedAt = _dateTimeProvider.UtcNow
        };

        // 3. Create associated profile
        if (request.Role == UserRole.Doctor)
        {
            user.Doctor = new Domain.Models.Doctor
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                CreatedAt = _dateTimeProvider.UtcNow,
                IsActive = true
            };
        }
        else if (request.Role == UserRole.Patient)
        {
            user.Patient = new Domain.Models.Patient
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                CreatedAt = _dateTimeProvider.UtcNow,
                IsActive = true,
                PrimaryDoctorId = null // Intentionally null by design
            };
        }

        // 4. Save to database
        await _userRepository.AddAsync(user);

        // 5. Generate Token
        string fullName = $"{request.FirstName} {request.LastName}";
        string token = _jwtTokenGenerator.GenerateToken(user, fullName);

        // 6. Return Result
        return new RegisterResult(
            UserId: user.Id,
            Email: user.Email,
            Role: user.Role.ToString(),
            Token: token);
    }
}
