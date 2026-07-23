namespace Contracts.Doctor.Request;

public record UpdateDoctorProfileRequest(
    string LicenseNumber,
    string Speciality);
