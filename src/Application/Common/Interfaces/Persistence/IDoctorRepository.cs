using Domain.Models;

namespace Application.Common.Interfaces.Persistence;

public interface IDoctorRepository
{
    Task<List<Doctor>> GetAllActiveAsync();
    Task<Doctor?> GetByIdAsync(Guid doctorId);
    Task<Guid?> GetDoctorIdByUserIdAsync(Guid userId);
    Task<Doctor?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);
    Task<Doctor?> GetOwnedDoctorAsync(
        Guid doctorId,
        Guid userId,
        CancellationToken cancellationToken);
    Task UpdateVerificationAsync(Guid doctorId, bool isVerified, string? curp, string? ineNumber, CancellationToken cancellationToken = default);
    Task UpdateProfileAsync(Guid doctorId, string licenseNumber, string speciality, CancellationToken cancellationToken = default);
}
