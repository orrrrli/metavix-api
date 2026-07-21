using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.LinkRequest.Commands;
using Application.UseCases.LinkRequest.Common;

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
        // 1. Authorize
        if (_currentUser.UserId is not { } userId)
            return AuthErrors.Forbidden;

        // 2. Find the link request
        var linkRequest = await _requestRepository.GetByIdAsync(request.RequestId);
        if (linkRequest is null)
        {
            return LinkRequestErrors.RequestNotFound;
        }

        // 3. Verify the caller owns the patient on the request.
        //    "Not found" and "not yours" are collapsed into Forbidden to
        //    close the patient-ID enumeration oracle.
        var patient = await _patientRepository.GetOwnedPatientAsync(
            linkRequest.PatientId, userId, cancellationToken);
        if (patient is null)
            return AuthErrors.Forbidden;

        // 4. Revoke the request (fails if not accepted)
        if (!linkRequest.Revoke(_timeProvider.GetUtcNow().UtcDateTime))
        {
            return LinkRequestErrors.NotAccepted;
        }
        if (!await _requestRepository.UpdateAsync(linkRequest))
        {
            return LinkRequestErrors.NotAccepted;
        }

        // 5. Remove the doctor from the patient
        patient.DetachPrimaryDoctor(linkRequest.DoctorId, clearMrn: false, _timeProvider.GetUtcNow().UtcDateTime);
        await _patientRepository.UpdateAsync(patient);

        return new LinkRequestResult(
            linkRequest.Id,
            linkRequest.PatientId,
            linkRequest.DoctorId,
            linkRequest.Status.ToString(),
            linkRequest.CreatedAt);
    }
}
