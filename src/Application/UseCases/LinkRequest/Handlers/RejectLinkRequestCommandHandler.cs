using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.LinkRequest.Commands;
using Application.UseCases.LinkRequest.Common;
using Domain.Enums;

namespace Application.UseCases.LinkRequest.Handlers;

internal sealed class RejectLinkRequestCommandHandler
    : IRequestHandler<RejectLinkRequestCommand, ErrorOr<LinkRequestResult>>
{
    private readonly IPatientDoctorRequestRepository _requestRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RejectLinkRequestCommandHandler(
        IPatientDoctorRequestRepository requestRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _requestRepository = requestRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ErrorOr<LinkRequestResult>> Handle(
        RejectLinkRequestCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Find the link request
        var linkRequest = await _requestRepository.GetByIdAsync(request.RequestId);
        if (linkRequest is null)
        {
            return LinkRequestErrors.RequestNotFound;
        }

        // 2. Verify it is still pending
        if (linkRequest.Status != RequestStatus.Pending)
        {
            return LinkRequestErrors.NotPending;
        }

        // 3. Reject the request
        linkRequest.Status = RequestStatus.Rejected;
        linkRequest.ResolvedAt = _dateTimeProvider.UtcNow;
        await _requestRepository.UpdateAsync(linkRequest);

        return new LinkRequestResult(
            linkRequest.Id,
            linkRequest.PatientId,
            linkRequest.DoctorId,
            linkRequest.Status.ToString(),
            linkRequest.CreatedAt);
    }
}
