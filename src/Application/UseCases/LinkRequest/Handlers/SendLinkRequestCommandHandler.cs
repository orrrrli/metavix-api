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
    private readonly IDateTimeProvider _dateTimeProvider;

    public SendLinkRequestCommandHandler(
        IPatientDoctorRequestRepository requestRepository,
        IDoctorRepository doctorRepository,
        IPatientRepository patientRepository,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _requestRepository = requestRepository;
        _doctorRepository = doctorRepository;
        _patientRepository = patientRepository;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ErrorOr<LinkRequestResult>> Handle(
        SendLinkRequestCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return AuthErrors.Forbidden;

        var callerPatientId = await _patientRepository.GetPatientIdByUserIdAsync(_currentUser.UserId.Value);
        if (callerPatientId != request.PatientId)
            return AuthErrors.Forbidden;

        // 1. Verify doctor exists
        var doctor = await _doctorRepository.GetByIdAsync(request.DoctorId);
        if (doctor is null)
        {
            return DoctorErrors.DoctorNotFound;
        }

        // 2. Verify patient exists
        var patient = await _patientRepository.GetByIdAsync(request.PatientId);
        if (patient is null)
        {
            return PatientErrors.PatientsNotFound;
        }

        // 3. Check if patient already has a doctor
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
            CreatedAt = _dateTimeProvider.UtcNow
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
