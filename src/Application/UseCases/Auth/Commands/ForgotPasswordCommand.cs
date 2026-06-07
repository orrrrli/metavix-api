namespace Application.UseCases.Auth.Commands;

public sealed record ForgotPasswordCommand(string Email) : IRequest<ErrorOr<Unit>>;
