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

        // 1. Find the link request. Same enumeration-oracle guard as
        //    RevokeDoctorAccessCommandHandler — see step 2 there.
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
        // Unlike Revoke, this handler authorizes against the doctor and only
        // loads the patient after persisting the Unlinked transition, so a
        // deleted patient is a no-op rather than Forbidden.
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
