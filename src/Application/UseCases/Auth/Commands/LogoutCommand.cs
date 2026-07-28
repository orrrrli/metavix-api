using Application.Common.Messaging;

namespace Application.UseCases.Auth.Commands;

public sealed record LogoutCommand(string RefreshToken) : ICommand<ErrorOr<Deleted>>;
