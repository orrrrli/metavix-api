namespace Application.Common.Messaging;

/// <remarks>
/// A command whose handler needs more than a single final SaveChangesAsync —
/// an intermediate IUnitOfWork.FlushAsync to validate an
/// optimistic-concurrency-guarded mutation before performing a dependent
/// mutation later in the same handler (see
/// Application.Common.Exceptions.ConcurrencyConflictException), or otherwise
/// several persistence steps that must commit or roll back together.
/// TransactionBehavior wraps these in ITransactionalExecutor (manual
/// transaction, retried as a whole by EF's execution strategy on a
/// transient failure — see EfTransactionalExecutor).
///
/// Not related by inheritance to <see cref="ICommand{TResponse}"/> — see
/// that type's remarks for why the two are mutually exclusive siblings.
/// A command with a single load → mutate → save flow, even across several
/// entities in the same SaveChangesAsync, belongs on ICommand instead: one
/// SaveChangesAsync is already atomic without a manual transaction, and
/// letting EF replay the whole handler (not just the write) on a retry is
/// unnecessary — and unsafe for handlers with non-idempotent external calls
/// (e.g. a single-use OAuth code).
/// </remarks>
public interface ITransactionalCommand<TResponse> : IRequest<TResponse>
    where TResponse : IErrorOr;
