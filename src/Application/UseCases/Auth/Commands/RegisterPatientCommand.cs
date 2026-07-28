using Application.UseCases.Auth.Common;
using Application.Common.Messaging;

namespace Application.UseCases.Auth.Commands;

public sealed record RegisterPatientCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password) : ITransactionalCommand<ErrorOr<RegisterResult>>;
