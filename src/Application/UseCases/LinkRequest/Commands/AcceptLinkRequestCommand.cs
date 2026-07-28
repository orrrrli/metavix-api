using Application.UseCases.LinkRequest.Common;
using Application.Common.Messaging;

namespace Application.UseCases.LinkRequest.Commands;

public sealed record AcceptLinkRequestCommand(
    Guid RequestId,
    string? MedicalRecordNumber) : ITransactionalCommand<ErrorOr<LinkRequestResult>>;
