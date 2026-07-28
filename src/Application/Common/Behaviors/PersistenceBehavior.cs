using Application.Common.Interfaces.Persistence;
using Application.Common.Messaging;

namespace Application.Common.Behaviors;

/// <remarks>
/// Wraps every ICommand in a single trailing SaveChangesAsync — no manual
/// transaction, no ITransactionalExecutor. A single SaveChangesAsync is
/// already atomic in EF Core, and AppDbContext's EnableRetryOnFailure covers
/// it directly, so a transient failure retries only the write, never the
/// handler. Commands with an intermediate flush, several persistence steps
/// that must commit/roll back together, or non-idempotent external calls
/// (see ITransactionalCommand's remarks) use TransactionBehavior instead.
/// </remarks>
public class PersistenceBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        if (response.IsError)
            return response;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }
}
