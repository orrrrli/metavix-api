using Application.UseCases.Auth.Commands;
using Application.UseCases.Auth.Common;
using Application.UseCases.Auth.Handlers;

namespace Application.Tests.Auth;

public class LoginCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly ILoginAttemptTracker _attemptTracker = Substitute.For<ILoginAttemptTracker>();

    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(
            _userRepository,
            _refreshTokenRepository,
            _passwordHasher,
            _jwtTokenGenerator,
            _timeProvider,
            _attemptTracker);
    }

    [Fact]
    public async Task Handle_WhenEmailIsBlocked_ReturnsTooManyFailedAttempts()
    {
        // Arrange
        var command = new LoginCommand("test@mail.com", "password123");
        _attemptTracker.IsBlocked(command.Email).Returns(true);

        // Act
        ErrorOr<LoginResult> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.TooManyFailedAttempts.Code);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ReturnsInvalidCredentials()
    {
        // Arrange
        var command = new LoginCommand("noexiste@mail.com", "password123");
        _attemptTracker.IsBlocked(command.Email).Returns(false);
        _userRepository.GetByEmailAsync(command.Email).Returns((User?)null);

        // Act
        ErrorOr<LoginResult> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.InvalidCredentials.Code);
        _attemptTracker.Received(1).RegisterFailure(command.Email);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoPasswordHash_ReturnsGoogleAccountOnly()
    {
        // Arrange
        var command = new LoginCommand("google@mail.com", "password123");
        var user = BuildUser(passwordHash: string.Empty);

        _attemptTracker.IsBlocked(command.Email).Returns(false);
        _userRepository.GetByEmailAsync(command.Email).Returns(user);

        // Act
        ErrorOr<LoginResult> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.GoogleAccountOnly.Code);
    }

    [Fact]
    public async Task Handle_WhenPasswordIsWrong_ReturnsInvalidCredentials()
    {
        // Arrange
        var command = new LoginCommand("test@mail.com", "wrongpassword");
        var user = BuildUser(passwordHash: "hashed_password");

        _attemptTracker.IsBlocked(command.Email).Returns(false);
        _userRepository.GetByEmailAsync(command.Email).Returns(user);
        _passwordHasher.Verify(command.Password, user.PasswordHash).Returns(false);

        // Act
        ErrorOr<LoginResult> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.InvalidCredentials.Code);
        _attemptTracker.Received(1).RegisterFailure(command.Email);
    }

    [Fact]
    public async Task Handle_WhenAccountIsInactive_ReturnsAccountInactive()
    {
        // Arrange
        var command = new LoginCommand("test@mail.com", "password123");
        var user = BuildUser(passwordHash: "hashed_password", isActive: false);

        _attemptTracker.IsBlocked(command.Email).Returns(false);
        _userRepository.GetByEmailAsync(command.Email).Returns(user);
        _passwordHasher.Verify(command.Password, user.PasswordHash).Returns(true);

        // Act
        ErrorOr<LoginResult> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.AccountInactive.Code);
    }

    [Fact]
    public async Task Handle_WhenCredentialsAreValid_ReturnsLoginResult()
    {
        // Arrange
        var command = new LoginCommand("test@mail.com", "password123");
        var user = BuildUser(passwordHash: "hashed_password");
        var now = DateTime.UtcNow;

        _attemptTracker.IsBlocked(command.Email).Returns(false);
        _userRepository.GetByEmailAsync(command.Email).Returns(user);
        _passwordHasher.Verify(command.Password, user.PasswordHash).Returns(true);
        _jwtTokenGenerator.GenerateToken(user, Arg.Any<string>()).Returns("access_token");
        _jwtTokenGenerator.GenerateRefreshToken().Returns("refresh_token");
        _timeProvider.SetUtcNow(now);

        // Act
        ErrorOr<LoginResult> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Email.Should().Be(user.Email);
        result.Value.AccessToken.Should().Be("access_token");
        result.Value.RefreshToken.Should().Be("refresh_token");
        _attemptTracker.Received(1).ResetAttempts(command.Email);
        await _refreshTokenRepository.Received(1).AddAsync(Arg.Any<RefreshToken>());
    }

    private static User BuildUser(string passwordHash = "hashed_password", bool isActive = true) => new()
    {
        Id           = Guid.NewGuid(),
        Email        = "test@mail.com",
        PasswordHash = passwordHash,
        Role         = UserRole.Patient,
        IsActive     = isActive,
        CreatedAt    = DateTime.UtcNow,
        Patient      = new Patient
        {
            Id        = Guid.NewGuid(),
            FirstName = "Juan",
            LastName  = "Pérez",
            Email     = "test@mail.com",
        }
    };
}
