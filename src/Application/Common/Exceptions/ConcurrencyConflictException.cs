namespace Application.Common.Exceptions;

/// <remarks>
/// Thrown by IUnitOfWork.FlushAsync/SaveChangesAsync when a concurrent
/// writer already committed a competing change to the same
/// optimistic-concurrency-guarded row. Infrastructure translates the
/// EF-specific DbUpdateConcurrencyException into this type so Application
/// handlers can catch a conflict without referencing EF Core.
/// </remarks>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
