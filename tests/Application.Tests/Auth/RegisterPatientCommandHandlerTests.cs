using Application.UseCases.Auth.Commands;
using Application.UseCases.Auth.Common;
using Application.UseCases.Auth.Handlers;

namespace Application.Tests.Auth;

public class RegisterPatientCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private readonly RegisterPatientCommandHandler _handler;

    public RegisterPatientCommandHandlerTests()
    {
        _handler = new RegisterPatientCommandHandler(
            _userRepository,
            _refreshTokenRepository,
            _passwordHasher,
            _jwtTokenGenerator,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ReturnsEmailAlreadyExists()
    {
        // Arrange
        var command = new RegisterPatientCommand("Juan", "Pérez", "juan@mail.com", "password123");
        _userRepository.ExistsByEmailAsync(command.Email).Returns(true);

        // Act
        ErrorOr<RegisterResult> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.EmailAlreadyExists.Code);
    }

    [Fact]
    public async Task Handle_WhenEmailIsAvailable_ReturnsRegisterResult()
    {
        // Arrange
        var command = new RegisterPatientCommand("Juan", "Pérez", "juan@mail.com", "password123");
        var now = DateTime.UtcNow;

        _userRepository.ExistsByEmailAsync(command.Email).Returns(false);
        _passwordHasher.Hash(command.Password).Returns("hashed_password");
        _jwtTokenGenerator.GenerateToken(Arg.Any<User>(), Arg.Any<string>()).Returns("access_token");
        _jwtTokenGenerator.GenerateRefreshToken().Returns("refresh_token");
        _dateTimeProvider.UtcNow.Returns(now);

        // Act
        ErrorOr<RegisterResult> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Email.Should().Be(command.Email);
        result.Value.Role.Should().Be(UserRole.Patient.ToString());
        result.Value.Token.Should().Be("access_token");
        result.Value.RefreshToken.Should().Be("refresh_token");
    }

    [Fact]
    public async Task Handle_WhenEmailIsAvailable_SavesUserAndRefreshToken()
    {
        // Arrange
        var command = new RegisterPatientCommand("Juan", "Pérez", "juan@mail.com", "password123");
        var now = DateTime.UtcNow;

        _userRepository.ExistsByEmailAsync(command.Email).Returns(false);
        _passwordHasher.Hash(command.Password).Returns("hashed_password");
        _jwtTokenGenerator.GenerateToken(Arg.Any<User>(), Arg.Any<string>()).Returns("access_token");
        _jwtTokenGenerator.GenerateRefreshToken().Returns("refresh_token");
        _dateTimeProvider.UtcNow.Returns(now);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _userRepository.Received(1).AddAsync(Arg.Is<User>(u =>
            u.Email        == command.Email &&
            u.PasswordHash == "hashed_password" &&
            u.Role         == UserRole.Patient &&
            u.IsActive     == true));

        await _refreshTokenRepository.Received(1).AddAsync(Arg.Is<RefreshToken>(rt =>
            rt.Token     == "refresh_token" &&
            rt.ExpiresAt == now.AddDays(7)));
    }

    [Fact]
    public async Task Handle_WhenEmailIsAvailable_HashesPassword()
    {
        // Arrange
        var command = new RegisterPatientCommand("Juan", "Pérez", "juan@mail.com", "password123");

        _userRepository.ExistsByEmailAsync(command.Email).Returns(false);
        _passwordHasher.Hash(command.Password).Returns("hashed_password");
        _jwtTokenGenerator.GenerateToken(Arg.Any<User>(), Arg.Any<string>()).Returns("access_token");
        _jwtTokenGenerator.GenerateRefreshToken().Returns("refresh_token");
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordHasher.Received(1).Hash(command.Password);
    }
}
