using Application.UseCases.LinkRequest.Common;

namespace Application.UseCases.LinkRequest.Commands;

public sealed record AcceptLinkRequestCommand(
    Guid RequestId,
    string? MedicalRecordNumber) : IRequest<ErrorOr<LinkRequestResult>>;
