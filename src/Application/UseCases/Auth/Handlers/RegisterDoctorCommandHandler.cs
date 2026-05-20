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
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RegisterDoctorCommandHandler(
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
        RegisterDoctorCommand request,
        CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email))
            return AuthErrors.EmailAlreadyExists;

        Guid userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.Doctor,
            IsActive = true,
            CreatedAt = _dateTimeProvider.UtcNow,
            Doctor = new Domain.Models.Doctor
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                CreatedAt = _dateTimeProvider.UtcNow,
                IsActive = true
            }
        };

        await _userRepository.AddAsync(user);

        string fullName = $"{request.FirstName} {request.LastName}";
        string token = _jwtTokenGenerator.GenerateToken(user, fullName);

        return new RegisterResult(
            UserId: user.Id,
            Email: user.Email,
            Role: user.Role.ToString(),
            Token: token);
    }
}
