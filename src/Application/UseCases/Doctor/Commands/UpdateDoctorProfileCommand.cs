using Application.UseCases.Doctor.Common;

namespace Application.UseCases.Doctor.Commands;

public sealed record UpdateDoctorProfileCommand(
    string LicenseNumber,
    string Speciality) : IRequest<ErrorOr<DoctorProfileResult>>;
