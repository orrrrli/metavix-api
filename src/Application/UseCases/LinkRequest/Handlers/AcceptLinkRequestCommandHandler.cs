using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.LinkRequest.Commands;
using Application.UseCases.LinkRequest.Common;
using Domain.Enums;

namespace Application.UseCases.LinkRequest.Handlers;

internal sealed class AcceptLinkRequestCommandHandler
    : IRequestHandler<AcceptLinkRequestCommand, ErrorOr<LinkRequestResult>>
{
    private readonly IPatientDoctorRequestRepository _requestRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AcceptLinkRequestCommandHandler(
        IPatientDoctorRequestRepository requestRepository,
        IPatientRepository patientRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _requestRepository = requestRepository;
        _patientRepository = patientRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ErrorOr<LinkRequestResult>> Handle(
        AcceptLinkRequestCommand request,
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

        // 3. Accept the request
        linkRequest.Status = RequestStatus.Accepted;
        linkRequest.ResolvedAt = _dateTimeProvider.UtcNow;
        await _requestRepository.UpdateAsync(linkRequest);

        // 4. Link the patient to the doctor
        var patient = await _patientRepository.GetByIdAsync(linkRequest.PatientId);
        if (patient is not null)
        {
            patient.PrimaryDoctorId = linkRequest.DoctorId;
            patient.UpdatedAt = _dateTimeProvider.UtcNow;
            await _patientRepository.UpdateAsync(patient);
        }

        return new LinkRequestResult(
            linkRequest.Id,
            linkRequest.PatientId,
            linkRequest.DoctorId,
            linkRequest.Status.ToString(),
            linkRequest.CreatedAt);
    }
}
