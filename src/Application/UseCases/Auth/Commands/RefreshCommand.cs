namespace Application.UseCases.Auth.Commands;

public sealed record RefreshCommand(string RefreshToken) : IRequest<ErrorOr<Common.RefreshResult>>;
