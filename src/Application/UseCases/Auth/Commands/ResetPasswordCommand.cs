namespace Application.UseCases.Auth.Commands;

public sealed record ResetPasswordCommand(
    string Token,
    string NewPassword) : IRequest<ErrorOr<Unit>>;
