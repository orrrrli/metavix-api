using Application.Common.Behaviors;
using Application.Common.Messaging;
using MediatR;

namespace Application.Tests.Behaviors;

public class TransactionBehaviorTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITransactionalExecutor _transactionalExecutor = Substitute.For<ITransactionalExecutor>();
    private readonly TransactionBehavior<FakeTransactionalCommand, ErrorOr<int>> _behavior;

    public TransactionBehaviorTests()
    {
        _behavior = new TransactionBehavior<FakeTransactionalCommand, ErrorOr<int>>(
            _transactionalExecutor, _unitOfWork);

        // Default: execute the operation as-is, mirroring what
        // EfTransactionalExecutor does around a successful call.
        _transactionalExecutor
            .ExecuteAsync(Arg.Any<Func<CancellationToken, Task<ErrorOr<int>>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task<ErrorOr<int>>>>()(callInfo.Arg<CancellationToken>()));
    }

    [Fact]
    public async Task Handle_WhenSuccess_SavesChangesAndReturnsResponse()
    {
        RequestHandlerDelegate<ErrorOr<int>> next = () => Task.FromResult<ErrorOr<int>>(42);

        var result = await _behavior.Handle(new FakeTransactionalCommand(), next, CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task Handle_WhenBusinessError_DoesNotSaveChanges()
    {
        RequestHandlerDelegate<ErrorOr<int>> next = () => Task.FromResult<ErrorOr<int>>(Error.Validation());

        var result = await _behavior.Handle(new FakeTransactionalCommand(), next, CancellationToken.None);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DelegatesExecutionToTransactionalExecutor()
    {
        RequestHandlerDelegate<ErrorOr<int>> next = () => Task.FromResult<ErrorOr<int>>(1);

        await _behavior.Handle(new FakeTransactionalCommand(), next, CancellationToken.None);

        await _transactionalExecutor.Received(1).ExecuteAsync(
            Arg.Any<Func<CancellationToken, Task<ErrorOr<int>>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToExecutor()
    {
        using var cts = new CancellationTokenSource();
        RequestHandlerDelegate<ErrorOr<int>> next = () => Task.FromResult<ErrorOr<int>>(1);

        await _behavior.Handle(new FakeTransactionalCommand(), next, cts.Token);

        await _transactionalExecutor.Received(1).ExecuteAsync(
            Arg.Any<Func<CancellationToken, Task<ErrorOr<int>>>>(), cts.Token);
    }
}

internal sealed record FakeTransactionalCommand : ITransactionalCommand<ErrorOr<int>>;
