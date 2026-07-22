using Application.Common.Interfaces.Persistence;
using Application.UseCases.Patient.Common;

namespace Infrastructure.Persistence;

public class PatientRepository : IPatientRepository
{
    private readonly AppDbContext _dbContext;

    public PatientRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<PatientResult?> GetPatientByPatientId(Guid patientId)
    {
        var patient = await _dbContext.Patients
            .FirstOrDefaultAsync(x => x.Id == patientId);

        if (patient is null) return null;

        return new PatientResult(
            patient.Id,
            patient.FirstName,
            patient.LastName,
            patient.MedicalRecordNumber);
    }

    public async Task<Patient?> GetByIdAsync(Guid patientId)
    {
        return await _dbContext.Patients.FirstOrDefaultAsync(x => x.Id == patientId);
    }

    public async Task UpdateAsync(Patient patient)
    {
        // If the entity is already tracked (loaded via GetByIdAsync without
        // AsNoTracking), the change tracker already knows which columns changed
        // and will emit a targeted UPDATE — calling .Update() here would instead
        // mark every property Modified and rewrite all columns (§4.4). Only
        // attach when the instance is detached (e.g. loaded via AsNoTracking).
        var entry = _dbContext.Entry(patient);
        if (entry.State == EntityState.Detached)
            _dbContext.Patients.Update(patient);

        await _dbContext.SaveChangesAsync();
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
    public async Task<Patient?> GetOwnedPatientAsync(
        Guid patientId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Patients
            .AsNoTracking()
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
