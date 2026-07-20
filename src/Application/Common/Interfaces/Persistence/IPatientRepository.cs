using Application.UseCases.Patient.Common;
using Application.UseCases.Patient.Queries;

namespace Application.Common.Interfaces.Persistence;

public interface IPatientRepository
{
    Task<List<PatientResult>> GetAllPatientByDoctorId(Guid doctorId);
    Task<PatientResult?> GetPatientByPatientId(Guid patientId);
    Task<Domain.Models.Patient?> GetByIdAsync(Guid patientId);
    Task UpdateAsync(Domain.Models.Patient patient);
    Task<Guid?> GetPatientIdByUserIdAsync(Guid userId);
    Task<Domain.Models.Patient?> GetOwnedPatientAsync(
        Guid patientId,
        Guid userId,
        CancellationToken cancellationToken);
    Task<bool> ExistsByMedicalRecordNumberAsync(string medicalRecordNumber, CancellationToken cancellationToken = default);
}
