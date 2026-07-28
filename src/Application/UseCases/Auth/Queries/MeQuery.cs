using Application.UseCases.Auth.Common;
using Application.Common.Messaging;

namespace Application.UseCases.Auth.Queries;

public sealed record MeQuery : IQuery<ErrorOr<MeResult>>;
