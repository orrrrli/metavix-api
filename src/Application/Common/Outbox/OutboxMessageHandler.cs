using System.Text.Json;

namespace Application.Common.Outbox;

/// <remarks>
/// Base for outbox handlers: deserializes Payload to TPayload and delegates
/// to HandleAsync(TPayload, ...). New use cases implement this instead of
/// IOutboxMessageHandler directly.
/// </remarks>
public abstract class OutboxMessageHandler<TPayload> : IOutboxMessageHandler
{
    public abstract string Type { get; }

    public async Task HandleAsync(string payload, CancellationToken cancellationToken)
    {
        var typedPayload = JsonSerializer.Deserialize<TPayload>(payload)
            ?? throw new InvalidOperationException(
                $"Outbox message of type '{Type}' has a payload that deserialized to null.");

        await HandleAsync(typedPayload, cancellationToken);
    }

    protected abstract Task HandleAsync(TPayload payload, CancellationToken cancellationToken);
}
