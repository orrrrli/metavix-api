using Application.Common.Messaging;

namespace Application.UseCases.Auth.Commands;

public sealed record ForgotPasswordCommand(string Email) : ITransactionalCommand<ErrorOr<Unit>>;
