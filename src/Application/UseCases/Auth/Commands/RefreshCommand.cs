using Application.Common.Messaging;

namespace Application.UseCases.Auth.Commands;

public sealed record RefreshCommand(string RefreshToken) : ICommand<ErrorOr<Common.RefreshResult>>;
