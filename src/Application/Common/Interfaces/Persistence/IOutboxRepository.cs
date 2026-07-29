using Domain.Models;

namespace Application.Common.Interfaces.Persistence;

/// <remarks>
/// AddAsync only tracks the message — no SaveChangesAsync. Callers add it to
/// the same DbContext as their other writes so PersistenceBehavior/
/// TransactionBehavior commits the outbox row atomically with the write that
/// triggered it. If the caller's SaveChangesAsync never runs (validation
/// error, exception), the message is never written — no orphaned message and
/// no dispatch without a corresponding committed change.
/// </remarks>
public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
