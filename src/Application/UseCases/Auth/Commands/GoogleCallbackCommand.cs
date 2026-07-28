using Application.UseCases.Auth.Common;
using Application.Common.Messaging;

namespace Application.UseCases.Auth.Commands;

public sealed record GoogleCallbackCommand(string Code, string State)
    : ITransactionalCommand<ErrorOr<LoginResult>>;
