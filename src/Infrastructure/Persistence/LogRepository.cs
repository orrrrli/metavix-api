using Application.Common.Interfaces.Persistence;
using Application.Common.Models;
using Application.UseCases.Admin.Common;
using Infrastructure.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class LogRepository : ILogRepository
{
    private readonly AppDbContext _dbContext;

    public LogRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedResult<LogEntryResult>> GetLogsAsync(
        string? level,
        string? endpoint,
        string? userId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize)
    {
        IQueryable<LogEntry> query = _dbContext.Logs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(level))
            query = query.Where(l => l.Level == level);

        if (!string.IsNullOrWhiteSpace(endpoint))
            query = query.Where(l => l.Endpoint != null && l.Endpoint.Contains(endpoint));

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(l => l.UserId == userId);

        if (from is not null)
            query = query.Where(l => l.RaiseDate >= from);

        if (to is not null)
            query = query.Where(l => l.RaiseDate <= to);

        int total = await query.CountAsync();

        List<LogEntryResult> data = await query
            .OrderByDescending(l => l.RaiseDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LogEntryResult(
                l.Id,
                l.Message,
                l.Level,
                l.RaiseDate,
                l.Exception,
                l.HttpMethod,
                l.Endpoint,
                l.CorrelationId,
                l.UserId,
                l.Role))
            .ToListAsync();

        return new PaginatedResult<LogEntryResult>(data, total, page, pageSize);
    }

    public async Task<List<LogEntryResult>> GetByCorrelationIdAsync(string correlationId)
    {
        return await _dbContext.Logs
            .Where(l => l.CorrelationId == correlationId)
            .OrderBy(l => l.RaiseDate)
            .Select(l => new LogEntryResult(
                l.Id,
                l.Message,
                l.Level,
                l.RaiseDate,
                l.Exception,
                l.HttpMethod,
                l.Endpoint,
                l.CorrelationId,
                l.UserId,
                l.Role))
            .ToListAsync();
    }
}
