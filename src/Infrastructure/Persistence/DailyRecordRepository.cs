using Application.Common.Interfaces.Persistence;
using Domain.Models;

namespace Infrastructure.Persistence;

public class DailyRecordRepository : IDailyRecordRepository
{
    private readonly AppDbContext _dbContext;

    public DailyRecordRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(DailyRecord record)
    {
        await _dbContext.DailyRecords.AddAsync(record);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<DailyRecord>> GetAllByPatientIdAsync(Guid patientId)
    {
        return await _dbContext.DailyRecords
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.RecordDate)
            .ThenByDescending(x => x.RecordTime)
            .ToListAsync();
    }

    public async Task<DailyRecord?> GetByIdAsync(Guid recordId)
    {
        return await _dbContext.DailyRecords
            .FirstOrDefaultAsync(x => x.Id == recordId);
    }
}
