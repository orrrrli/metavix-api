using Application.Common.Models;
using Application.UseCases.Admin.Common;
using Application.Common.Messaging;

namespace Application.UseCases.Admin.Queries;

public record GetLogsQuery(
    string? Level,
    string? Endpoint,
    string? UserId,
    DateTime? From,
    DateTime? To,
    int Page,
    int PageSize) : IQuery<ErrorOr<PaginatedResult<LogEntryResult>>>;
