using Application.UseCases.LinkRequest.Common;

namespace Application.UseCases.LinkRequest.Commands;

public sealed record UnlinkPatientCommand(
    Guid RequestId) : IRequest<ErrorOr<LinkRequestResult>>;
