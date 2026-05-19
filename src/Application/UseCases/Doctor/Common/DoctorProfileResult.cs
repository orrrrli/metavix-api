namespace Application.UseCases.Doctor.Common;

public sealed record DoctorProfileResult(
    Guid Id,
    string FirstName,
    string LastName,
    string LicenseNumber,
    string Speciality,
    string Email,
    string? Phone,
    bool IsActive,
    DateTime CreatedAt);
