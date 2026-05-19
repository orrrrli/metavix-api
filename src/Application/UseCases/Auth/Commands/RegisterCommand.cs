using Domain.Enums;
using Application.UseCases.Auth.Common;

namespace Application.UseCases.Auth.Commands;

public sealed record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    UserRole Role) : IRequest<ErrorOr<RegisterResult>>;
