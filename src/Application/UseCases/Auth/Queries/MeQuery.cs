using Application.UseCases.Auth.Common;

namespace Application.UseCases.Auth.Queries;

public sealed record MeQuery : IRequest<ErrorOr<MeResult>>;
