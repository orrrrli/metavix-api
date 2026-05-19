namespace Application.UseCases.Auth.Commands;

public sealed record LoginCommand(
    string Email,
    string Password) : IRequest<ErrorOr<Common.LoginResult>>;
