using Application.Common.Authorization;
using Application.Common.Errors;
using Application.Common.Exceptions;
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
    private readonly IUnitOfWork _unitOfWork;

    public UnlinkPatientCommandHandler(
        IPatientDoctorRequestRepository requestRepository,
        IPatientRepository patientRepository,
        IDoctorRepository doctorRepository,
        ICurrentUserService currentUser,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
    {
        _requestRepository = requestRepository;
        _patientRepository = patientRepository;
        _doctorRepository = doctorRepository;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<ErrorOr<LinkRequestResult>> Handle(
        UnlinkPatientCommand request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserAccess.RequireUserId(_currentUser, out var userId) is { } userIdError)
            return userIdError;

        // Same enumeration-oracle guard as RevokeDoctorAccessCommandHandler.
        var linkRequest = await _requestRepository.GetByIdAsync(request.RequestId);
        if (linkRequest is null)
        {
            return AuthErrors.Forbidden;
        }

        var callerDoctor = await _doctorRepository.GetOwnedDoctorAsync(
            linkRequest.DoctorId, userId, cancellationToken);
        if (callerDoctor is null)
            return AuthErrors.Forbidden;

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // NotAccepted is returned identically whether Unlink() rejected the
        // state transition or the flushed write lost a concurrency race —
        // callers don't need to distinguish "wrong state" from "stale write".
        // Flushing immediately (inside the transaction TransactionBehavior
        // opened) detects the conflict before the patient is touched.
        if (!linkRequest.Unlink(now))
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

        // Unlike Revoke, the patient is loaded after persistence, so a missing
        // patient is a no-op rather than Forbidden.
        // TODO: IPatientRepository.GetByIdAsync doesn't accept a
        // CancellationToken yet; thread it through once the interface is
        // extended (same gap in RevokeDoctorAccessCommandHandler).
        var patient = await _patientRepository.GetByIdAsync(linkRequest.PatientId);
        if (patient is not null)
        {
            patient.DetachPrimaryDoctor(linkRequest.DoctorId, clearMrn: true, now);
        }

        return new LinkRequestResult(
            linkRequest.Id,
            linkRequest.PatientId,
            linkRequest.DoctorId,
            linkRequest.Status.ToString(),
            linkRequest.CreatedAt);
    }
}
