using Application.UseCases.Auth.Common;

namespace Application.UseCases.Auth.Commands;

public sealed record RegisterDoctorCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string LicenseNumber,
    string Speciality) : IRequest<ErrorOr<RegisterResult>>;
