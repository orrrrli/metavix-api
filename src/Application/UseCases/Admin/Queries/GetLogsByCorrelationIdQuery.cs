using Application.UseCases.Admin.Common;

namespace Application.UseCases.Admin.Queries;

public record GetLogsByCorrelationIdQuery(string CorrelationId)
    : IRequest<ErrorOr<List<LogEntryResult>>>;
