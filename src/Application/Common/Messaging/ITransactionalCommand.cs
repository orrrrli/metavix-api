namespace Application.Common.Messaging;

/// <remarks>
/// Marks a command as one that mutates persistent state. Only requests
/// implementing this interface (not the broader <see cref="ICommand{TResponse}"/>)
/// are wrapped by <c>PersistenceBehavior</c>, so a command that only
/// orchestrates (e.g. sends an email, calls an external service) without
/// touching the database does not pay for — or trigger — a SaveChangesAsync.
/// </remarks>
public interface ITransactionalCommand<TResponse> : ICommand<TResponse>;
