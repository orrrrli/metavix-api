using Domain.Models;

namespace Application.Common.Interfaces.Persistence;

public interface IPatientDoctorRequestRepository
{
    Task AddAsync(PatientDoctorRequest request);
    Task<PatientDoctorRequest?> GetByIdAsync(Guid id);
    Task<List<PatientDoctorRequest>> GetPendingByDoctorIdAsync(Guid doctorId);
    Task<bool> HasPendingRequestAsync(Guid patientId, Guid doctorId);
    Task UpdateAsync(PatientDoctorRequest request);
}
