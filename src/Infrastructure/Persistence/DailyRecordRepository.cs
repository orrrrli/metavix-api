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
            .Include(r => r.GlucoseReadings)
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.RecordDate)
            .ThenByDescending(x => x.RecordTime)
            .ToListAsync();
    }

    public async Task<DailyRecord?> GetByIdAsync(Guid recordId)
    {
        return await _dbContext.DailyRecords
            .Include(r => r.GlucoseReadings)
            .FirstOrDefaultAsync(x => x.Id == recordId);
    }

    public async Task<DailyRecord?> GetLatestByPatientIdAsync(Guid patientId)
    {
        return await _dbContext.DailyRecords
            .Include(r => r.GlucoseReadings)
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.RecordDate)
            .ThenByDescending(r => r.RecordTime)
            .FirstOrDefaultAsync();
    }

    public async Task<DailyRecord?> GetFirstByPatientIdAndDateAsync(
        Guid patientId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.DailyRecords
            .Where(r => r.PatientId == patientId && r.RecordDate == date)
            .OrderBy(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<DailyRecord>> GetByPatientIdInRangeAsync(
        Guid patientId,
        DateOnly dateFrom,
        DateOnly dateTo,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.DailyRecords
            .Include(r => r.GlucoseReadings)
            .Where(r => r.PatientId == patientId && r.RecordDate >= dateFrom && r.RecordDate <= dateTo)
            .OrderByDescending(r => r.RecordDate)
            .ThenByDescending(r => r.RecordTime)
            .ToListAsync(cancellationToken);
    }
}
