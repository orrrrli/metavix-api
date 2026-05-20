using Application.Common.Models;
using Application.UseCases.Admin.Common;

namespace Application.Common.Interfaces.Persistence;

public interface ILogRepository
{
    Task<PaginatedResult<LogEntryResult>> GetLogsAsync(
        string? level,
        string? endpoint,
        string? userId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize);

    Task<List<LogEntryResult>> GetByCorrelationIdAsync(string correlationId);
}
