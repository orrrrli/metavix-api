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

    public async Task<List<PatientResult>> GetAllPatientByDoctorId(Guid doctorId)
    {
        return await _dbContext.Patients
            .Where(x => x.PrimaryDoctorId == doctorId)
            .Select(x => new PatientResult(
                x.FirstName,
                x.LastName,
                x.MedicalRecordNumber))
            .ToListAsync();
    }

    public async Task<PatientResult?> GetPatientByPatientId(Guid patientId)
    {
        var patient = await _dbContext.Patients
            .FirstOrDefaultAsync(x => x.Id == patientId);

        if (patient is null) return null;

        return new PatientResult(
            patient.FirstName,
            patient.LastName,
            patient.MedicalRecordNumber);
    }

    public async Task<Domain.Models.Patient?> GetByIdAsync(Guid patientId)
    {
        return await _dbContext.Patients.FirstOrDefaultAsync(x => x.Id == patientId);
    }

    public async Task UpdateAsync(Domain.Models.Patient patient)
    {
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
}