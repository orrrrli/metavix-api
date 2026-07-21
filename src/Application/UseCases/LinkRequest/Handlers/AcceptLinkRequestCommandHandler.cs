using Application.Common.Errors;
using Application.Common.Generators;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.LinkRequest.Commands;
using Application.UseCases.LinkRequest.Common;
using Domain.Enums;

namespace Application.UseCases.LinkRequest.Handlers;

internal sealed class AcceptLinkRequestCommandHandler
    : IRequestHandler<AcceptLinkRequestCommand, ErrorOr<LinkRequestResult>>
{
    private const int MaxAutoAssignAttempts = 5;

    private readonly IPatientDoctorRequestRepository _requestRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _timeProvider;

    public AcceptLinkRequestCommandHandler(
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
        AcceptLinkRequestCommand request,
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

        var callerDoctor = await _doctorRepository.GetOwnedDoctorAsync(
            linkRequest.DoctorId, _currentUser.UserId.Value, cancellationToken);
        if (callerDoctor is null)
            return AuthErrors.Forbidden;

        // 2. Verify it is still pending
        if (linkRequest.Status != RequestStatus.Pending)
        {
            return LinkRequestErrors.NotPending;
        }

        // 3. Resolve the MRN to assign:
        //    - If the doctor provided one, validate uniqueness.
        //    - Otherwise auto-assign the next available for the current year.
        string? assignedMrn;
        if (!string.IsNullOrEmpty(request.MedicalRecordNumber))
        {
            if (await _patientRepository.ExistsByMedicalRecordNumberAsync(
                    request.MedicalRecordNumber, cancellationToken))
                return LinkRequestErrors.MrnAlreadyAssigned;
            assignedMrn = request.MedicalRecordNumber;
        }
        else
        {
            var autoMrn = await TryAutoAssignAsync(cancellationToken);
            if (autoMrn is null)
            {
                // Exhausted retries — surface as a transient error so the
                // client can retry. The unique index still guarantees no
                // duplicates are ever persisted.
                return LinkRequestErrors.MrnAutoAssignFailed;
            }
            assignedMrn = autoMrn;
        }

        // 4. Load the patient BEFORE mutating anything. If the patient was
        //    deleted between sending and accepting the request, bail out with a
        //    concrete error instead of accepting the request and then silently
        //    skipping the link — that path used to leave the request Accepted
        //    with no doctor ever assigned to the (missing) patient and still
        //    reported success.
        var patient = await _patientRepository.GetByIdAsync(linkRequest.PatientId);
        if (patient is null)
            return PatientErrors.PatientNotFound;

        // 5. Accept the request
        if (!linkRequest.Accept(_timeProvider.GetUtcNow().UtcDateTime))
        {
            return LinkRequestErrors.NotPending;
        }
        await _requestRepository.UpdateAsync(linkRequest);

        // 6. Link the patient to the doctor and assign the MRN
        patient.AssignDoctorAndMrn(linkRequest.DoctorId, assignedMrn, _timeProvider.GetUtcNow().UtcDateTime);
        await _patientRepository.UpdateAsync(patient);

        return new LinkRequestResult(
            linkRequest.Id,
            linkRequest.PatientId,
            linkRequest.DoctorId,
            linkRequest.Status.ToString(),
            linkRequest.CreatedAt);
    }

    /// <summary>
    /// Generates a timestamp-derived MRN candidate and re-checks uniqueness.
    /// The DB unique index is the ultimate backstop — this loop just avoids
    /// the noisy 500 in the rare case two candidates land in the same
    /// millisecond.
    /// </summary>
    private async Task<string?> TryAutoAssignAsync(CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        for (int attempt = 0; attempt < MaxAutoAssignAttempts; attempt++)
        {
            var candidate = MrnGenerator.Suggest(now.AddMilliseconds(attempt));
            if (!await _patientRepository.ExistsByMedicalRecordNumberAsync(candidate, cancellationToken))
                return candidate;
        }
        return null;
    }
}
