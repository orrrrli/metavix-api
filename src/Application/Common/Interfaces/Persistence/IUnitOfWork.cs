namespace Application.Common.Interfaces.Persistence;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <remarks>
    /// Flushes pending changes mid-handler, inside the transaction opened by
    /// TransactionBehavior via ITransactionalExecutor, without committing it.
    /// Lets a handler validate an optimistic-concurrency-guarded mutation
    /// (e.g. PatientDoctorRequest's Version token) before performing
    /// dependent mutations later in the same handler — see
    /// Application.Common.Exceptions.ConcurrencyConflictException.
    /// </remarks>
    Task<int> FlushAsync(CancellationToken cancellationToken = default);
}
