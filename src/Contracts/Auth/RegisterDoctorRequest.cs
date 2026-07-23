namespace Contracts.Auth;

public record RegisterDoctorRequest(
    string FirstName,
    string? MiddleName,
    string PaternalLastName,
    string MaternalLastName,
    string Email,
    string Password,
    string LicenseNumber,
    string Speciality);
