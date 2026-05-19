using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Security;
using Application.Common.Interfaces.Services;
using Application.UseCases.Auth.Commands;
using Application.UseCases.Auth.Common;
using Domain.Enums;

namespace Application.UseCases.Auth.Handlers;

internal sealed class LoginCommandHandler
    : IRequestHandler<LoginCommand, ErrorOr<LoginResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LoginCommandHandler(
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

    public async Task<ErrorOr<LoginResult>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Find user by email (includes Doctor/Patient navigation)
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user is null)
        {
            return AuthErrors.InvalidCredentials;
        }

        // 2. Verify password
        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return AuthErrors.InvalidCredentials;
        }

        // 3. Check if account is active
        if (!user.IsActive)
        {
            return AuthErrors.AccountInactive;
        }

        // 4. Resolve full name based on role
        string fullName = user.Role switch
        {
            UserRole.Doctor => user.Doctor is not null
                ? $"{user.Doctor.FirstName} {user.Doctor.LastName}"
                : user.Email,
            UserRole.Patient => user.Patient is not null
                ? $"{user.Patient.FirstName} {user.Patient.LastName}"
                : user.Email,
            _ => user.Email
        };

        // 5. Generate JWT token
        string token = _jwtTokenGenerator.GenerateToken(user, fullName);

        // 6. Return result
        return new LoginResult(
            AccessToken: token,
            ExpiresAt: _dateTimeProvider.UtcNow.AddMinutes(15),
            Email: user.Email,
            Role: user.Role.ToString(),
            FullName: fullName);
    }
}
