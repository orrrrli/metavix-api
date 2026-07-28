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
    }

    // AsTracking: callers (Accept/Reject/Revoke/Unlink handlers) mutate the
    // returned request's state via Accept()/Reject()/Revoke()/Unlink() and
    // rely on MarkForUpdate + IUnitOfWork.FlushAsync — see AppDbContext's
    // global QueryTrackingBehavior.NoTracking default.
    public async Task<PatientDoctorRequest?> GetByIdAsync(Guid id)
    {
        return await _dbContext.PatientDoctorRequests
            .AsTracking()
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

    public void MarkForUpdate(PatientDoctorRequest request)
    {
        // request is already tracked (loaded via GetByIdAsync, AsTracking),
        // so the change tracker already knows which columns Accept/Reject/
        // Revoke/Unlink changed — it will emit a targeted UPDATE without
        // needing an explicit .Update() call.
        var entry = _dbContext.Entry(request);

        // Bump the optimistic-concurrency token so the emitted UPDATE both
        // writes a new Version and carries `WHERE "Version" = @original` (EF
        // uses the original value it read). A concurrent writer that already
        // committed leaves @original stale → zero rows updated → the caller's
        // IUnitOfWork.FlushAsync throws ConcurrencyConflictException.
        entry.Property<long>("Version").CurrentValue += 1;
    }
}
