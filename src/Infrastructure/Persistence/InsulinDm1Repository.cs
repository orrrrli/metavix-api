using Application.Common.Interfaces.Persistence;
using Domain.Models;

namespace Infrastructure.Persistence;

public class InsulinDm1Repository : IInsulinDm1Repository
{
    private readonly AppDbContext _dbContext;

    public InsulinDm1Repository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<InsulinDm1Profile?> GetProfileByPatientIdAsync(Guid patientId)
    {
        return await _dbContext.InsulinDm1Profiles
            .FirstOrDefaultAsync(p => p.PatientId == patientId);
    }

    public async Task UpsertProfileAsync(InsulinDm1Profile profile)
    {
        var tracked = _dbContext.InsulinDm1Profiles.Local
            .FirstOrDefault(p => p.Id == profile.Id);

        if (tracked is null)
        {
            var exists = await _dbContext.InsulinDm1Profiles
                .AsNoTracking()
                .AnyAsync(p => p.Id == profile.Id);

            if (exists)
                _dbContext.InsulinDm1Profiles.Update(profile);
            else
                await _dbContext.InsulinDm1Profiles.AddAsync(profile);
        }
        else
        {
            _dbContext.Entry(tracked).CurrentValues.SetValues(profile);
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task AddRecordAsync(InsulinDm1Record record)
    {
        await _dbContext.InsulinDm1Records.AddAsync(record);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<InsulinDm1Record>> GetRecordsByPatientIdAsync(Guid patientId)
    {
        return await _dbContext.InsulinDm1Records
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.RecordDate)
            .ToListAsync();
    }

    public async Task<InsulinDm1Record?> GetRecordByIdAsync(Guid recordId)
    {
        return await _dbContext.InsulinDm1Records
            .FirstOrDefaultAsync(r => r.Id == recordId);
    }

    public async Task DeleteRecordAsync(InsulinDm1Record record)
    {
        _dbContext.InsulinDm1Records.Remove(record);
        await _dbContext.SaveChangesAsync();
    }
}
