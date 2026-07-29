using Application.Common.Behaviors;
using Application.Common.Messaging;
using MediatR;

namespace Application.Tests.Behaviors;

public class PersistenceBehaviorTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly PersistenceBehavior<FakeCommand, ErrorOr<int>> _behavior;

    public PersistenceBehaviorTests()
    {
        _behavior = new PersistenceBehavior<FakeCommand, ErrorOr<int>>(_unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenSuccess_CallsSaveChangesExactlyOnce()
    {
        RequestHandlerDelegate<ErrorOr<int>> next = () => Task.FromResult<ErrorOr<int>>(42);

        var result = await _behavior.Handle(new FakeCommand(), next, CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task Handle_WhenBusinessError_DoesNotSaveChanges()
    {
        RequestHandlerDelegate<ErrorOr<int>> next = () => Task.FromResult<ErrorOr<int>>(Error.Validation());

        var result = await _behavior.Handle(new FakeCommand(), next, CancellationToken.None);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenHandlerThrows_DoesNotSaveChangesAndPropagates()
    {
        RequestHandlerDelegate<ErrorOr<int>> next = () => throw new InvalidOperationException("boom");

        Func<Task> act = () => _behavior.Handle(new FakeCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToSaveChanges()
    {
        using var cts = new CancellationTokenSource();
        RequestHandlerDelegate<ErrorOr<int>> next = () => Task.FromResult<ErrorOr<int>>(1);

        await _behavior.Handle(new FakeCommand(), next, cts.Token);

        await _unitOfWork.Received(1).SaveChangesAsync(cts.Token);
    }
}

internal sealed record FakeCommand : ICommand<ErrorOr<int>>;
