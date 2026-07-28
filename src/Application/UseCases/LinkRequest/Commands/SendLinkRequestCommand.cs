using Application.UseCases.LinkRequest.Common;
using Application.Common.Messaging;

namespace Application.UseCases.LinkRequest.Commands;

public sealed record SendLinkRequestCommand(
    Guid PatientId,
    Guid DoctorId) : ICommand<ErrorOr<LinkRequestResult>>;
