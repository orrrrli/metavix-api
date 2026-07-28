namespace Application.Common.Messaging;

/// <remarks>
/// A command whose handler performs a single load → mutate/add → return
/// sequence, ending in one PersistenceBehavior-driven SaveChangesAsync. Not
/// related by inheritance to <see cref="ITransactionalCommand{TResponse}"/> —
/// the two are mutually exclusive sibling markers so a request implements
/// exactly one, never both (avoiding double persistence from two pipeline
/// behaviors both matching the same request).
/// </remarks>
public interface ICommand<TResponse> : IRequest<TResponse>
    where TResponse : IErrorOr;
