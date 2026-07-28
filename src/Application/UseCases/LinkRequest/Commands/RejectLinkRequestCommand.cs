using Application.UseCases.LinkRequest.Common;
using Application.Common.Messaging;

namespace Application.UseCases.LinkRequest.Commands;

public sealed record RejectLinkRequestCommand(
    Guid RequestId) : ITransactionalCommand<ErrorOr<LinkRequestResult>>;
