namespace Application.UseCases.Doctor.Common;

public sealed record DoctorProfileResult(
    Guid Id,
    string FirstName,
    string? MiddleName,
    string PaternalLastName,
    string MaternalLastName,
    string LicenseNumber,
    string Speciality,
    string Email,
    string? Phone,
    string? Curp,
    string? IneNumber,
    bool IsVerified,
    bool IsActive,
    DateTime CreatedAt);
