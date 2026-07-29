using Application.Common.Messaging;

namespace Application.UseCases.Auth.Commands;

public sealed record ResetPasswordCommand(
    string Token,
    string NewPassword) : ICommand<ErrorOr<Unit>>;
