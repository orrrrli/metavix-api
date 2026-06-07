using Application.UseCases.Auth.Common;

namespace Application.UseCases.Auth.Commands;

public sealed record GoogleCallbackCommand(string Code, string State)
    : IRequest<ErrorOr<LoginResult>>;
