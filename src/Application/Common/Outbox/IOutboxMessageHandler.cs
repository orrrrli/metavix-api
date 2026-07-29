namespace Application.Common.Outbox;

/// <remarks>
/// Non-generic dispatch surface: the worker resolves handlers by
/// OutboxMessage.Type (a stable string key, see <see cref="OutboxMessageTypes"/>)
/// and calls this without needing to know the payload type at the call site.
/// Implementations deserialize the payload themselves — see
/// <see cref="OutboxMessageHandler{TPayload}"/> for the typical shape.
/// </remarks>
public interface IOutboxMessageHandler
{
    string Type { get; }

    Task HandleAsync(string payload, CancellationToken cancellationToken);
}
