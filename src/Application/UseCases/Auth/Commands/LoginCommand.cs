using Application.Common.Messaging;

namespace Application.UseCases.Auth.Commands;

public sealed record LoginCommand(
    string Email,
    string Password) : ICommand<ErrorOr<Common.LoginResult>>;
