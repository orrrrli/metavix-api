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

    // AsTracking: UpsertInsulinProfileCommandHandler mutates the returned
    // profile in-memory (or builds a new one when null) and relies on
    // PersistenceBehavior to commit — see AppDbContext's global
    // QueryTrackingBehavior.NoTracking default.
    public async Task<InsulinDm1Profile?> GetProfileByPatientIdAsync(Guid patientId)
    {
        return await _dbContext.InsulinDm1Profiles
            .AsTracking()
            .FirstOrDefaultAsync(p => p.PatientId == patientId);
    }

    // The caller always passes either the entity just loaded by
    // GetProfileByPatientIdAsync (already tracked — the change tracker will
    // pick up the mutations on its own) or a brand-new profile (never
    // tracked, needs an explicit Add).
    public Task UpsertProfileAsync(InsulinDm1Profile profile)
    {
        if (_dbContext.Entry(profile).State == EntityState.Detached)
            _dbContext.InsulinDm1Profiles.Add(profile);

        return Task.CompletedTask;
    }

    public async Task AddRecordAsync(InsulinDm1Record record)
    {
        await _dbContext.InsulinDm1Records.AddAsync(record);
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

    public Task DeleteRecordAsync(InsulinDm1Record record)
    {
        _dbContext.InsulinDm1Records.Remove(record);
        return Task.CompletedTask;
    }
}
