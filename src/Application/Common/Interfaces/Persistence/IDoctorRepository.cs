using Domain.Models;

namespace Application.Common.Interfaces.Persistence;

public interface IDoctorRepository
{
    Task<List<Doctor>> GetAllActiveAsync();
    Task<Doctor?> GetByIdAsync(Guid doctorId);
    Task<Guid?> GetDoctorIdByUserIdAsync(Guid userId);
}
