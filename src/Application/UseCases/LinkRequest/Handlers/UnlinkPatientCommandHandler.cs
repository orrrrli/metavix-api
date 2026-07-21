using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.LinkRequest.Commands;
using Application.UseCases.LinkRequest.Common;

namespace Application.UseCases.LinkRequest.Handlers;

internal sealed class UnlinkPatientCommandHandler
    : IRequestHandler<UnlinkPatientCommand, ErrorOr<LinkRequestResult>>
{
    private readonly IPatientDoctorRequestRepository _requestRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _timeProvider;

    public UnlinkPatientCommandHandler(
        IPatientDoctorRequestRepository requestRepository,
        IPatientRepository patientRepository,
        IDoctorRepository doctorRepository,
        ICurrentUserService currentUser,
        TimeProvider timeProvider)
    {
        _requestRepository = requestRepository;
        _patientRepository = patientRepository;
        _doctorRepository = doctorRepository;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<ErrorOr<LinkRequestResult>> Handle(
        UnlinkPatientCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
            return AuthErrors.Forbidden;

        // 1. Find the link request. A missing request returns Forbidden (not
        //    RequestNotFound) so that "request doesn't exist" and "request
        //    exists but isn't your patient" (step 2) are indistinguishable —
        //    otherwise the caller could probe requestIds for existence.
        var linkRequest = await _requestRepository.GetByIdAsync(request.RequestId);
        if (linkRequest is null)
        {
            return AuthErrors.Forbidden;
        }

        // 2. Verify the caller owns the doctor on the request.
        var callerDoctor = await _doctorRepository.GetOwnedDoctorAsync(
            linkRequest.DoctorId, userId, cancellationToken);
        if (callerDoctor is null)
            return AuthErrors.Forbidden;

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // 3. Unlink the patient (fails if not accepted)
        if (!linkRequest.Unlink(now))
        {
            return LinkRequestErrors.NotAccepted;
        }
        if (!await _requestRepository.UpdateAsync(linkRequest))
        {
            return LinkRequestErrors.NotAccepted;
        }

        // 4. Remove the doctor from the patient and clear the MRN.
        // The MRN belongs to the doctor-patient RELATION, not the patient,
        // so once the relation ends the value is freed for re-use.
        //
        // Unlike RevokeDoctorAccessCommandHandler — which loads the patient
        // during authorization (step 3) and therefore fails the whole
        // operation with Forbidden if the patient is gone — this handler
        // authorizes against the doctor and only loads the patient here, after
        // the request has already been persisted as Unlinked. If the patient
        // was deleted between steps 1 and 4, there is nothing left to detach:
        // silently skipping the mutation is correct, not a swallowed error.
        var patient = await _patientRepository.GetByIdAsync(linkRequest.PatientId);
        if (patient is not null)
        {
            patient.DetachPrimaryDoctor(linkRequest.DoctorId, clearMrn: true, now);
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
