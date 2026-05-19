using Application.UseCases.LinkRequest.Common;

namespace Application.UseCases.LinkRequest.Commands;

public sealed record RejectLinkRequestCommand(
    Guid RequestId) : IRequest<ErrorOr<LinkRequestResult>>;
