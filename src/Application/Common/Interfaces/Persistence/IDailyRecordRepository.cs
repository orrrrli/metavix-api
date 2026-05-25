using Domain.Models;

namespace Application.Common.Interfaces.Persistence;

public interface IDailyRecordRepository
{
    Task AddAsync(DailyRecord record);
    Task<List<DailyRecord>> GetAllByPatientIdAsync(Guid patientId);
    Task<DailyRecord?> GetByIdAsync(Guid recordId);
    Task<DailyRecord?> GetLatestByPatientIdAsync(Guid patientId);
}
