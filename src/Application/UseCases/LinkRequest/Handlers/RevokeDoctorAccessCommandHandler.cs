using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.LinkRequest.Commands;
using Application.UseCases.LinkRequest.Common;
using Domain.Enums;

namespace Application.UseCases.LinkRequest.Handlers;

internal sealed class RevokeDoctorAccessCommandHandler
    : IRequestHandler<RevokeDoctorAccessCommand, ErrorOr<LinkRequestResult>>
{
    private readonly IPatientDoctorRequestRepository _requestRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RevokeDoctorAccessCommandHandler(
        IPatientDoctorRequestRepository requestRepository,
        IPatientRepository patientRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _requestRepository = requestRepository;
        _patientRepository = patientRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ErrorOr<LinkRequestResult>> Handle(
        RevokeDoctorAccessCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Find the link request
        var linkRequest = await _requestRepository.GetByIdAsync(request.RequestId);
        if (linkRequest is null)
        {
            return LinkRequestErrors.RequestNotFound;
        }

        // 2. Verify it is accepted (only accepted links can be revoked)
        if (linkRequest.Status != RequestStatus.Accepted)
        {
            return LinkRequestErrors.NotAccepted;
        }

        // 3. Revoke the request
        linkRequest.Status = RequestStatus.Revoked;
        linkRequest.ResolvedAt = _dateTimeProvider.UtcNow;
        await _requestRepository.UpdateAsync(linkRequest);

        // 4. Remove the doctor from the patient
        var patient = await _patientRepository.GetByIdAsync(linkRequest.PatientId);
        if (patient is not null && patient.PrimaryDoctorId == linkRequest.DoctorId)
        {
            patient.PrimaryDoctorId = null;
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
