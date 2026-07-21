using Application.Common.Authorization;
using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.LinkRequest.Commands;
using Application.UseCases.LinkRequest.Common;
using Domain.Models;

namespace Application.UseCases.LinkRequest.Handlers;

internal sealed class SendLinkRequestCommandHandler
    : IRequestHandler<SendLinkRequestCommand, ErrorOr<LinkRequestResult>>
{
    private readonly IPatientDoctorRequestRepository _requestRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _timeProvider;

    public SendLinkRequestCommandHandler(
        IPatientDoctorRequestRepository requestRepository,
        IDoctorRepository doctorRepository,
        IPatientRepository patientRepository,
        ICurrentUserService currentUser,
        TimeProvider timeProvider)
    {
        _requestRepository = requestRepository;
        _doctorRepository = doctorRepository;
        _patientRepository = patientRepository;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<ErrorOr<LinkRequestResult>> Handle(
        SendLinkRequestCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Authenticate + load the owned patient (see PatientAccess).
        var access = await PatientAccess.RequireOwnedPatientAsync(
            _currentUser, _patientRepository, request.PatientId, cancellationToken);
        if (access.IsError)
            return access.Errors;

        var patient = access.Value;

        // 3. Verify doctor exists. Returning DoctorNotFound here is safe and is
        //    NOT an enumeration oracle: doctors are publicly discoverable by any
        //    authenticated patient via /patient/get-all-doctors, which already
        //    exposes every doctor id. So confirming existence leaks nothing the
        //    directory does not, and a distinct error gives the patient better
        //    feedback than a blanket Forbidden. (Contrast with patient ids, which
        //    are never listable and so are collapsed into Forbidden above.)
        var doctor = await _doctorRepository.GetByIdAsync(request.DoctorId);
        if (doctor is null)
        {
            return DoctorErrors.DoctorNotFound;
        }

        // 4. Enforce link invariants. These live here (after ownership + doctor
        //    existence are confirmed) because they are guarding against a
        //    duplicate/conflicting link for a patient we already know the caller
        //    owns — they are not access-control checks and must not run before
        //    step 2, or they would leak information about patients the caller
        //    does not own.
        // 4a. A patient may only be linked to one primary doctor.
        if (patient.PrimaryDoctorId is not null)
        {
            return LinkRequestErrors.AlreadyLinked;
        }

        // 4b. Don't create a second request while one is still pending.
        if (await _requestRepository.HasPendingRequestAsync(request.PatientId, request.DoctorId))
        {
            return LinkRequestErrors.AlreadyPending;
        }

        // 5. Create request
        var linkRequest = new PatientDoctorRequest
        {
            Id = Guid.NewGuid(),
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            Status = Domain.Enums.RequestStatus.Pending,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        await _requestRepository.AddAsync(linkRequest);

        return new LinkRequestResult(
            linkRequest.Id,
            linkRequest.PatientId,
            linkRequest.DoctorId,
            linkRequest.Status.ToString(),
            linkRequest.CreatedAt);
    }
}
