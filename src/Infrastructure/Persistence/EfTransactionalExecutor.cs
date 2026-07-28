using Application.Common.Interfaces.Persistence;

namespace Infrastructure.Persistence;

/// <remarks>
/// AppDbContext has EnableRetryOnFailure configured (NpgsqlRetryingExecutionStrategy),
/// which requires manual transactions to run inside
/// Database.CreateExecutionStrategy().ExecuteAsync(...) — otherwise EF throws,
/// since a retry must replay the entire transactional unit, not a single
/// isolated command. This is the only place in the codebase that knows that.
/// </remarks>
internal sealed class EfTransactionalExecutor : ITransactionalExecutor
{
    private readonly AppDbContext _dbContext;

    public EfTransactionalExecutor(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<TResponse> ExecuteAsync<TResponse>(
        Func<CancellationToken, Task<TResponse>> operation,
        CancellationToken cancellationToken = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var response = await operation(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return response;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}
