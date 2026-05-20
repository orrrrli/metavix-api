namespace Application.UseCases.Admin.Common;

public record LogEntryResult(
    int Id,
    string Message,
    string Level,
    DateTime RaiseDate,
    string? Exception,
    string? HttpMethod,
    string? Endpoint,
    string? CorrelationId,
    string? UserId,
    string? Role);
