using Domain.Models;

namespace Application.Common.Interfaces.Persistence;

public interface IDoctorRepository
{
    Task<List<Doctor>> GetAllActiveAsync();
    Task<Doctor?> GetByIdAsync(Guid doctorId);

    /// <remarks>
    /// For "my profile" endpoints: the caller acts on their own doctor record,
    /// so a single by-userId lookup is the right granularity — no doctorId is
    /// supplied by the request. A null result means the authenticated user has
    /// no doctor profile yet, which is a missing resource, not a permissions
    /// failure (callers should surface DoctorNotFound, not Forbidden).
    /// </remarks>
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
