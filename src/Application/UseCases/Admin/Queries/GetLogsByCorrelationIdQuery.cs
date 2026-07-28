using Application.UseCases.Admin.Common;
using Application.Common.Messaging;

namespace Application.UseCases.Admin.Queries;

public record GetLogsByCorrelationIdQuery(string CorrelationId)
    : IQuery<ErrorOr<List<LogEntryResult>>>;
