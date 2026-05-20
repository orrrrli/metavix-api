using Application.Common.Models;
using Application.UseCases.Admin.Common;

namespace Application.UseCases.Admin.Queries;

public record GetLogsQuery(
    string? Level,
    string? Endpoint,
    string? UserId,
    DateTime? From,
    DateTime? To,
    int Page,
    int PageSize) : IRequest<ErrorOr<PaginatedResult<LogEntryResult>>>;
