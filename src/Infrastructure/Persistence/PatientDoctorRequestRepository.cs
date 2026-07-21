using Application.Common.Interfaces.Persistence;
using Domain.Enums;
using Domain.Models;

namespace Infrastructure.Persistence;

public class PatientDoctorRequestRepository : IPatientDoctorRequestRepository
{
    private readonly AppDbContext _dbContext;

    public PatientDoctorRequestRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(PatientDoctorRequest request)
    {
        await _dbContext.PatientDoctorRequests.AddAsync(request);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<PatientDoctorRequest?> GetByIdAsync(Guid id)
    {
        return await _dbContext.PatientDoctorRequests
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<PatientDoctorRequest>> GetPendingByDoctorIdAsync(Guid doctorId)
    {
        return await _dbContext.PatientDoctorRequests
            .Include(r => r.Patient)
            .Where(r => r.DoctorId == doctorId && r.Status == RequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<PatientDoctorRequest>> GetPendingByPatientIdAsync(Guid patientId)
    {
        return await _dbContext.PatientDoctorRequests
            .Include(r => r.Doctor)
            .Where(r => r.PatientId == patientId && r.Status == RequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<PatientDoctorRequest>> GetAcceptedByPatientIdAsync(Guid patientId)
    {
        return await _dbContext.PatientDoctorRequests
            .Include(r => r.Doctor)
            .Where(r => r.PatientId == patientId && r.Status == RequestStatus.Accepted)
            .OrderByDescending(r => r.ResolvedAt)
            .ToListAsync();
    }

    public async Task<List<PatientDoctorRequest>> GetAcceptedByDoctorIdAsync(Guid doctorId)
    {
        return await _dbContext.PatientDoctorRequests
            .Include(r => r.Patient)
            .Where(r => r.DoctorId == doctorId && r.Status == RequestStatus.Accepted)
            .OrderByDescending(r => r.ResolvedAt)
            .ToListAsync();
    }

    public async Task<bool> HasPendingRequestAsync(Guid patientId, Guid doctorId)
    {
        return await _dbContext.PatientDoctorRequests
            .AnyAsync(r => r.PatientId == patientId
                        && r.DoctorId == doctorId
                        && r.Status == RequestStatus.Pending);
    }

    public async Task<bool> IsAcceptedLinkAsync(Guid doctorId, Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PatientDoctorRequests
            .AnyAsync(r => r.DoctorId == doctorId
                        && r.PatientId == patientId
                        && r.Status == RequestStatus.Accepted, cancellationToken);
    }

    public async Task UpdateAsync(PatientDoctorRequest request)
    {
        // GetByIdAsync loads tracked entities (no AsNoTracking), so Accept /
        // Reject / Revoke mutate 2-3 properties and the change tracker already
        // knows which columns changed — it will emit a targeted UPDATE. Calling
        // .Update() here would instead mark every property Modified and rewrite
        // all columns. Only attach when the instance is detached.
        var entry = _dbContext.Entry(request);
        if (entry.State == EntityState.Detached)
            _dbContext.PatientDoctorRequests.Update(request);

        await _dbContext.SaveChangesAsync();
    }
}
