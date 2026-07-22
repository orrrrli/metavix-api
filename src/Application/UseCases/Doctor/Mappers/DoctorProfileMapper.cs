using Application.UseCases.Doctor.Common;
using DomainDoctor = Domain.Models.Doctor;

namespace Application.UseCases.Doctor.Mappers;

internal static class DoctorProfileMapper
{
    // licenseNumber/speciality are overridable because UpdateDoctorProfileCommandHandler
    // updates those two fields via a targeted ExecuteUpdate and the loaded `doctor` still
    // holds the pre-update values; the command's new values are passed in instead.
    public static DoctorProfileResult ToResult(
        DomainDoctor doctor,
        string? licenseNumber = null,
        string? speciality = null) => new(
        doctor.Id,
        doctor.FirstName,
        doctor.MiddleName,
        doctor.PaternalLastName,
        doctor.MaternalLastName,
        licenseNumber ?? doctor.LicenseNumber,
        speciality ?? doctor.Speciality,
        doctor.Email,
        doctor.Phone,
        doctor.Curp,
        doctor.IneNumber,
        doctor.IsVerified,
        doctor.IsActive,
        doctor.CreatedAt);
}
