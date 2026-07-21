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

    /// <remarks>
    /// For "my profile" endpoints: the caller acts on their own patient record,
    /// so a single by-userId lookup is the right granularity — no patientId is
    /// supplied by the request. A null result means the authenticated user has
    /// no patient profile yet, which is a missing resource, not a permissions
    /// failure (callers should surface PatientNotFound, not Forbidden).
    /// </remarks>
    Task<Domain.Models.Patient?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);
    Task<Domain.Models.Patient?> GetOwnedPatientAsync(
        Guid patientId,
        Guid userId,
        CancellationToken cancellationToken);
    Task<bool> ExistsByMedicalRecordNumberAsync(string medicalRecordNumber, CancellationToken cancellationToken = default);
}
