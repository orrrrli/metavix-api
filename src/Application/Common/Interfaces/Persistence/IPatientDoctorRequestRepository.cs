using Domain.Models;

namespace Application.Common.Interfaces.Persistence;

public interface IPatientDoctorRequestRepository
{
    Task AddAsync(PatientDoctorRequest request);
    Task<PatientDoctorRequest?> GetByIdAsync(Guid id);
    Task<List<PatientDoctorRequest>> GetPendingByDoctorIdAsync(Guid doctorId);
    Task<List<PatientDoctorRequest>> GetPendingByPatientIdAsync(Guid patientId);
    Task<List<PatientDoctorRequest>> GetAcceptedByPatientIdAsync(Guid patientId);
    Task<List<PatientDoctorRequest>> GetAcceptedByDoctorIdAsync(Guid doctorId);
    Task<bool> HasPendingRequestAsync(Guid patientId, Guid doctorId);
    Task<bool> IsAcceptedLinkAsync(Guid doctorId, Guid patientId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Persists a state transition on a tracked request. Returns false when an
    /// optimistic-concurrency conflict is detected (another caller committed a
    /// competing transition first) so the handler can surface the right
    /// not-in-expected-state error instead of double-applying the change.
    /// </summary>
    Task<bool> UpdateAsync(PatientDoctorRequest request);
}
