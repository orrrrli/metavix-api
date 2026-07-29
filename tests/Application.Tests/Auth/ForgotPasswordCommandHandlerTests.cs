using Application.Common.Interfaces.Persistence;
using Application.Common.Outbox;
using Application.Common.Settings;
using Application.UseCases.Auth.Commands;
using Application.UseCases.Auth.Handlers;
using Application.UseCases.Auth.Outbox;
using Domain.Models;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Tests.Auth;

public class ForgotPasswordCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordResetTokenRepository _tokenRepository = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IOutboxRepository _outboxRepository = Substitute.For<IOutboxRepository>();
    private readonly FakeTimeProvider _timeProvider = new();

    private readonly ForgotPasswordCommandHandler _handler;

    public ForgotPasswordCommandHandlerTests()
    {
        _handler = new ForgotPasswordCommandHandler(
            _userRepository,
            _tokenRepository,
            _outboxRepository,
            _timeProvider,
            Options.Create(new AppSettings { AppBaseUrl = "https://metavix.com.mx" }));
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ReturnsSuccessWithoutQueueingEmail()
    {
        // Arrange
        var command = new ForgotPasswordCommand("noexiste@mail.com");
        _userRepository.GetByEmailAsync(command.Email).Returns((User?)null);

        // Act
        ErrorOr<Unit> result = await _handler.Handle(command, CancellationToken.None);

        // Assert — always succeeds, prevents email enumeration
        result.IsError.Should().BeFalse();
        await _outboxRepository.DidNotReceive().AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserExists_TracksTokenAndQueuesOutboxMessage()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "user@mail.com" };
        var command = new ForgotPasswordCommand(user.Email);
        _userRepository.GetByEmailAsync(command.Email).Returns(user);

        // Act
        ErrorOr<Unit> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();

        await _tokenRepository.Received(1).AddAsync(Arg.Is<PasswordResetToken>(t => t.UserId == user.Id));

        await _outboxRepository.Received(1).AddAsync(
            Arg.Is<OutboxMessage>(m =>
                m.Type == OutboxMessageTypes.PasswordResetEmail &&
                m.Payload.Contains(user.Email) &&
                m.ProcessedAt == null),
            Arg.Any<CancellationToken>());
    }
}
