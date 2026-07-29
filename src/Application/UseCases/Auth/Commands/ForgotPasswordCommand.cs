using Application.Common.Messaging;

namespace Application.UseCases.Auth.Commands;

public sealed record ForgotPasswordCommand(string Email) : ICommand<ErrorOr<Unit>>;
