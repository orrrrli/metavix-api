using Application.UseCases.Doctor.Common;
using Application.Common.Messaging;

namespace Application.UseCases.Doctor.Commands;

public sealed record UpdateDoctorProfileCommand(
    string LicenseNumber,
    string Speciality) : ICommand<ErrorOr<DoctorProfileResult>>;
