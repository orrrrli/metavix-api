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
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _timeProvider;

    public RevokeDoctorAccessCommandHandler(
        IPatientDoctorRequestRepository requestRepository,
        IPatientRepository patientRepository,
        ICurrentUserService currentUser,
        TimeProvider timeProvider)
    {
        _requestRepository = requestRepository;
        _patientRepository = patientRepository;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<ErrorOr<LinkRequestResult>> Handle(
        RevokeDoctorAccessCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return AuthErrors.Forbidden;

        // 1. Find the link request
        var linkRequest = await _requestRepository.GetByIdAsync(request.RequestId);
        if (linkRequest is null)
        {
            return LinkRequestErrors.RequestNotFound;
        }

        var callerPatientId = await _patientRepository.GetPatientIdByUserIdAsync(_currentUser.UserId.Value);
        if (callerPatientId != linkRequest.PatientId)
            return AuthErrors.Forbidden;

        // 2. Verify it is accepted (only accepted links can be revoked)
        if (linkRequest.Status != RequestStatus.Accepted)
        {
            return LinkRequestErrors.NotAccepted;
        }

        // 3. Revoke the request
        linkRequest.Status = RequestStatus.Revoked;
        linkRequest.ResolvedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _requestRepository.UpdateAsync(linkRequest);

        // 4. Remove the doctor from the patient
        var patient = await _patientRepository.GetByIdAsync(linkRequest.PatientId);
        if (patient is not null)
        {
            patient.DetachPrimaryDoctor(linkRequest.DoctorId, clearMrn: false, _timeProvider.GetUtcNow().UtcDateTime);
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
