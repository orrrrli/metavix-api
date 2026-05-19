using Domain.Models;

namespace Application.Common.Interfaces.Persistence;

public interface ILabResultRepository
{
    Task AddAsync(LabResult result);
    Task<List<LabResult>> GetAllByPatientIdAsync(Guid patientId);
    Task<LabResult?> GetByIdAsync(Guid resultId);
}
