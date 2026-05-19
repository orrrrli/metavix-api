using Application.UseCases.LinkRequest.Common;

namespace Application.UseCases.LinkRequest.Commands;

public sealed record SendLinkRequestCommand(
    Guid PatientId,
    Guid DoctorId) : IRequest<ErrorOr<LinkRequestResult>>;
