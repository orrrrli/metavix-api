using Domain.Models;

namespace Application.Common.Interfaces.Persistence;

public interface IPatientDoctorRequestRepository
{
    Task AddAsync(PatientDoctorRequest request);
    Task<PatientDoctorRequest?> GetByIdAsync(Guid id);
    Task<List<PatientDoctorRequest>> GetPendingByDoctorIdAsync(Guid doctorId);
    Task<List<PatientDoctorRequest>> GetAcceptedByPatientIdAsync(Guid patientId);
    Task<List<PatientDoctorRequest>> GetAcceptedByDoctorIdAsync(Guid doctorId);
    Task<bool> HasPendingRequestAsync(Guid patientId, Guid doctorId);
    Task<bool> IsAcceptedLinkAsync(Guid doctorId, Guid patientId);
    Task UpdateAsync(PatientDoctorRequest request);
}
