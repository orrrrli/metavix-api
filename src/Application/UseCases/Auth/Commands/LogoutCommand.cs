namespace Application.UseCases.Auth.Commands;

public sealed record LogoutCommand(string RefreshToken) : IRequest<ErrorOr<Deleted>>;
