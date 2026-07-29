using Application.Common.Interfaces.Persistence;
using Application.Common.Messaging;

namespace Application.Common.Behaviors;

/// <remarks>
/// Wraps every ITransactionalCommand in a single DB transaction via
/// ITransactionalExecutor. The handler may call IUnitOfWork.FlushAsync
/// mid-handler to validate an optimistic-concurrency-guarded mutation before
/// performing dependent mutations later in the same handler (see
/// Application.Common.Exceptions.ConcurrencyConflictException) — that flush
/// participates in the same transaction, so a later failure still rolls back
/// everything, including the flushed change.
/// </remarks>
public class TransactionBehavior<TRequest, TResponse>(
    ITransactionalExecutor transactionalExecutor,
    IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ITransactionalCommand<TResponse>
    where TResponse : IErrorOr
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        return transactionalExecutor.ExecuteAsync(
            async ct =>
            {
                var response = await next();

                if (response.IsError)
                    return response;

                await unitOfWork.SaveChangesAsync(ct);

                return response;
            },
            cancellationToken);
    }
}
