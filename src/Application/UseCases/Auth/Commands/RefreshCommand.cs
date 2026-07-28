using Application.Common.Messaging;

namespace Application.UseCases.Auth.Commands;

public sealed record RefreshCommand(string RefreshToken) : ITransactionalCommand<ErrorOr<Common.RefreshResult>>;
