using Application.UseCases.LinkRequest.Common;

namespace Application.UseCases.LinkRequest.Commands;

public sealed record AcceptLinkRequestCommand(
    Guid RequestId) : IRequest<ErrorOr<LinkRequestResult>>;
