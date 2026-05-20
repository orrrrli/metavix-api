using Application.UseCases.Auth.Common;

namespace Application.UseCases.Auth.Commands;

public sealed record RegisterPatientCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password) : IRequest<ErrorOr<RegisterResult>>;
