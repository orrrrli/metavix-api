using Domain.Models;

namespace Application.Common.Interfaces.Persistence;

public interface IInsulinDm1Repository
{
    Task<InsulinDm1Profile?> GetProfileByPatientIdAsync(Guid patientId);
    Task UpsertProfileAsync(InsulinDm1Profile profile);
    Task AddRecordAsync(InsulinDm1Record record);
    Task<List<InsulinDm1Record>> GetRecordsByPatientIdAsync(Guid patientId);
    Task<InsulinDm1Record?> GetRecordByIdAsync(Guid recordId);
    Task DeleteRecordAsync(InsulinDm1Record record);
}
