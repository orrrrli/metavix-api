using Application.Common.Interfaces.Persistence;
using Domain.Models;

namespace Infrastructure.Persistence;

public class LabResultRepository : ILabResultRepository
{
    private readonly AppDbContext _dbContext;

    public LabResultRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(LabResult result)
    {
        await _dbContext.LabResults.AddAsync(result);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<LabResult>> GetAllByPatientIdAsync(Guid patientId)
    {
        return await _dbContext.LabResults
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.SampleDate)
            .ToListAsync();
    }

    public async Task<LabResult?> GetByIdAsync(Guid resultId)
    {
        return await _dbContext.LabResults
            .FirstOrDefaultAsync(x => x.Id == resultId);
    }

    public async Task<LabResult?> GetLatestByPatientIdAsync(Guid patientId)
    {
        return await _dbContext.LabResults
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.SampleDate)
            .FirstOrDefaultAsync();
    }
}
