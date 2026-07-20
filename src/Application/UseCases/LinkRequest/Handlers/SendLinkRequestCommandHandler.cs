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
        // 1. Authorize
        if (_currentUser.UserId is not { } userId)
            return AuthErrors.Forbidden;

        // 2. Load — single query resolves ownership + existence.
        //    "Not found" and "not yours" are collapsed into Forbidden to
        //    close the patient-ID enumeration oracle.
        var patient = await _patientRepository.GetOwnedPatientAsync(
            request.PatientId, userId, cancellationToken);
        if (patient is null)
            return AuthErrors.Forbidden;

        // 3. Verify doctor exists
        var doctor = await _doctorRepository.GetByIdAsync(request.DoctorId);
        if (doctor is null)
        {
            return DoctorErrors.DoctorNotFound;
        }

        // 4. Check if patient already has a doctor
        if (patient.PrimaryDoctorId is not null)
        {
            return LinkRequestErrors.AlreadyLinked;
        }

        // 4. Check if there is already a pending request
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
