using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.UseCases.Admin.Common;
using Application.UseCases.Admin.Queries;

namespace Application.UseCases.Admin.Handlers;

internal sealed class GetLogsByCorrelationIdQueryHandler
    : IRequestHandler<GetLogsByCorrelationIdQuery, ErrorOr<List<LogEntryResult>>>
{
    private readonly ILogRepository _logRepository;

    public GetLogsByCorrelationIdQueryHandler(ILogRepository logRepository)
    {
        _logRepository = logRepository;
    }

    public async Task<ErrorOr<List<LogEntryResult>>> Handle(
        GetLogsByCorrelationIdQuery request,
        CancellationToken cancellationToken)
    {
        List<LogEntryResult> entries = await _logRepository.GetByCorrelationIdAsync(request.CorrelationId);

        if (entries.Count == 0)
            return Error.NotFound("Log.NotFound", "No log entries found for this correlation ID.");

        return entries;
    }
}
