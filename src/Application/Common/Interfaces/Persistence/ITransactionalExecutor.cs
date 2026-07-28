namespace Application.Common.Interfaces.Persistence;

/// <remarks>
/// Runs an operation inside a resilient DB transaction boundary — knows
/// nothing about EF Core (DbContext, IExecutionStrategy, IDbContextTransaction).
/// Infrastructure implements the retry-safe transaction mechanics; Application
/// only expresses "run this inside a transaction".
/// </remarks>
public interface ITransactionalExecutor
{
    Task<TResponse> ExecuteAsync<TResponse>(
        Func<CancellationToken, Task<TResponse>> operation,
        CancellationToken cancellationToken = default);
}
