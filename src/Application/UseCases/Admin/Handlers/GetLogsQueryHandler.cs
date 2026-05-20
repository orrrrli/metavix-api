using Application.Common.Interfaces.Persistence;
using Application.Common.Models;
using Application.UseCases.Admin.Common;
using Application.UseCases.Admin.Queries;

namespace Application.UseCases.Admin.Handlers;

internal sealed class GetLogsQueryHandler
    : IRequestHandler<GetLogsQuery, ErrorOr<PaginatedResult<LogEntryResult>>>
{
    private readonly ILogRepository _logRepository;

    public GetLogsQueryHandler(ILogRepository logRepository)
    {
        _logRepository = logRepository;
    }

    public async Task<ErrorOr<PaginatedResult<LogEntryResult>>> Handle(
        GetLogsQuery request,
        CancellationToken cancellationToken)
    {
        return await _logRepository.GetLogsAsync(
            request.Level,
            request.Endpoint,
            request.UserId,
            request.From,
            request.To,
            request.Page,
            request.PageSize);
    }
}
