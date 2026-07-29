using Application.Common.Interfaces.Persistence;

namespace Infrastructure.Persistence;

public class PatientRepository : IPatientRepository
{
    private readonly AppDbContext _dbContext;

    public PatientRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    // AsTracking: GetByIdAsync backs write paths (UnlinkPatientCommandHandler,
    // AcceptLinkRequestCommandHandler, RevokeDoctorAccessCommandHandler,
    // UpdatePatientProfileCommandHandler all mutate the returned Patient and
    // rely on PersistenceBehavior to commit) — see AppDbContext's global
    // QueryTrackingBehavior.NoTracking default.
    public async Task<Patient?> GetByIdAsync(Guid patientId)
    {
        return await _dbContext.Patients.AsTracking().FirstOrDefaultAsync(x => x.Id == patientId);
    }

    public async Task<Guid?> GetPatientIdByUserIdAsync(Guid userId)
    {
        return await _dbContext.Patients
            .Where(p => p.UserId == userId)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<Patient?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    // Returns the Patient only if it exists AND belongs to userId.
    // Collapses "not found" and "not yours" into a single null so the
    // handler can't leak which patient IDs exist (enumeration oracle).
    //
    // AsTracking: shared by ~20 handlers via PatientAccess.RequireOwnedPatientAsync
    // — most are read-only queries, but a handful (UpdatePatientProfile,
    // AddLabResult, AddDailyRecord, UpsertInsulinProfile, AddInsulinRecord,
    // DeleteInsulinRecord, EvaluateGoals, SendLinkRequest) mutate the returned
    // Patient and rely on PersistenceBehavior to commit. A single tracked
    // FirstOrDefaultAsync is cheap enough that splitting this into a
    // read-only/write-path pair isn't worth the duplication.
    public async Task<Patient?> GetOwnedPatientAsync(
        Guid patientId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Patients
            .AsTracking()
            .FirstOrDefaultAsync(
                p => p.Id == patientId && p.UserId == userId,
                cancellationToken);
    }

    public async Task<bool> ExistsByMedicalRecordNumberAsync(
        string medicalRecordNumber,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Patients
            .AnyAsync(p => p.MedicalRecordNumber == medicalRecordNumber, cancellationToken);
    }
}
