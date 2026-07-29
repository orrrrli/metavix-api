using Application.Common.Outbox;
using Infrastructure.Common.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Outbox;

/// <remarks>
/// Polls OutboxMessages for unprocessed rows and dispatches each to the
/// IOutboxMessageHandler matching its Type. Runs in-process — fine for a
/// single API instance; would need a claim/lock column (e.g. "LockedUntil")
/// if the API ever scales to multiple instances polling the same table.
/// </remarks>
public sealed class OutboxProcessorService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize  = 20;
    private const int MaxAttempts = 5;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OutboxProcessorService> _logger;

    public OutboxProcessorService(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<OutboxProcessorService> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            await ProcessBatchAsync(stoppingToken);
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <remarks>Public for direct invocation from integration tests — avoids racing the polling timer.</remarks>
    public async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var handlers = scope.ServiceProvider.GetServices<IOutboxMessageHandler>()
            .ToDictionary(h => h.Type);

        var messages = await dbContext.OutboxMessages
            .AsTracking()
            .Where(m => m.ProcessedAt == null && m.Attempts < MaxAttempts)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            if (!handlers.TryGetValue(message.Type, out var handler))
            {
                message.Attempts++;
                message.LastError = $"No IOutboxMessageHandler registered for type '{message.Type}'.";
                _logger.LogWarning(
                    "No IOutboxMessageHandler registered for outbox message type {MessageType} (id {MessageId}), attempt {Attempt}/{MaxAttempts}",
                    message.Type, message.Id, message.Attempts, MaxAttempts);
                continue;
            }

            try
            {
                await handler.HandleAsync(message.Payload, cancellationToken);
                message.ProcessedAt = _timeProvider.GetUtcNow().UtcDateTime;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.Attempts++;
                message.LastError = ex.Message;
                _logger.LogError(
                    ex,
                    "Outbox message {MessageId} of type {MessageType} failed on attempt {Attempt}/{MaxAttempts}",
                    message.Id, message.Type, message.Attempts, MaxAttempts);
            }
        }

        if (messages.Count > 0)
            await dbContext.SaveChangesAsync(cancellationToken);
    }
}
