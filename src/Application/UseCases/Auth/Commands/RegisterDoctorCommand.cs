using Application.UseCases.Auth.Common;
using Application.Common.Messaging;

namespace Application.UseCases.Auth.Commands;

public sealed record RegisterDoctorCommand(
    string FirstName,
    string? MiddleName,
    string PaternalLastName,
    string MaternalLastName,
    string Email,
    string Password,
    string LicenseNumber,
    string Speciality) : ICommand<ErrorOr<RegisterResult>>;
