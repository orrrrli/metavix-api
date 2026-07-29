using Application.Common.Authorization;
using Application.Common.Errors;
using Application.Common.Exceptions;
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
    private readonly IUnitOfWork _unitOfWork;

    public RevokeDoctorAccessCommandHandler(
        IPatientDoctorRequestRepository requestRepository,
        IPatientRepository patientRepository,
        ICurrentUserService currentUser,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
    {
        _requestRepository = requestRepository;
        _patientRepository = patientRepository;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<ErrorOr<LinkRequestResult>> Handle(
        RevokeDoctorAccessCommand request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserAccess.RequireUserId(_currentUser, out var userId) is { } userIdError)
            return userIdError;

        // A missing request returns Forbidden (not RequestNotFound) so that
        // "request doesn't exist" and "request exists but isn't your patient"
        // (below) are indistinguishable — otherwise the caller could probe
        // requestIds for existence. UnlinkPatientCommandHandler applies the
        // same guard against the doctor instead of the patient.
        var linkRequest = await _requestRepository.GetByIdAsync(request.RequestId);
        if (linkRequest is null)
        {
            return AuthErrors.Forbidden;
        }

        // "Not found" and "not yours" are collapsed into Forbidden to close
        // the patient-ID enumeration oracle.
        var patient = await _patientRepository.GetOwnedPatientAsync(
            linkRequest.PatientId, userId, cancellationToken);
        if (patient is null)
            return AuthErrors.Forbidden;

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // NotAccepted is returned identically whether Revoke() rejected the
        // state transition or the flushed write lost a concurrency race —
        // callers don't need to distinguish "wrong state" from "stale write".
        // Flushing immediately (inside the transaction TransactionBehavior
        // opened) detects the conflict before the patient is touched.
        if (!linkRequest.Revoke(now))
        {
            return LinkRequestErrors.NotAccepted;
        }
        _requestRepository.MarkForUpdate(linkRequest);
        try
        {
            await _unitOfWork.FlushAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return LinkRequestErrors.NotAccepted;
        }

        // patient was loaded via GetOwnedPatientAsync (AsTracking) above —
        // the mutation below is picked up by EF's change tracker and
        // committed by the final SaveChangesAsync + commit in
        // TransactionBehavior.
        patient.DetachPrimaryDoctor(linkRequest.DoctorId, clearMrn: false, now);

        return new LinkRequestResult(
            linkRequest.Id,
            linkRequest.PatientId,
            linkRequest.DoctorId,
            linkRequest.Status.ToString(),
            linkRequest.CreatedAt);
    }
}
