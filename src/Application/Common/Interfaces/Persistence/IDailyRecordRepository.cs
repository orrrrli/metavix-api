using Domain.Models;

namespace Application.Common.Interfaces.Persistence;

public interface IDailyRecordRepository
{
    Task AddAsync(DailyRecord record);
    Task<List<DailyRecord>> GetAllByPatientIdAsync(Guid patientId);
    Task<DailyRecord?> GetByIdAsync(Guid recordId);
    Task<DailyRecord?> GetLatestByPatientIdAsync(Guid patientId);
    Task<DailyRecord?> GetFirstByPatientIdAndDateAsync(Guid patientId, DateOnly date, CancellationToken cancellationToken = default);
    Task<List<DailyRecord>> GetByPatientIdInRangeAsync(Guid patientId, DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken = default);
}
