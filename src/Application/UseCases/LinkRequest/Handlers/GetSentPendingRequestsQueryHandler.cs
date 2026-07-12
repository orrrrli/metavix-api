using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.LinkRequest.Common;
using Application.UseCases.LinkRequest.Queries;

namespace Application.UseCases.LinkRequest.Handlers;

internal sealed class GetSentPendingRequestsQueryHandler
    : IRequestHandler<GetSentPendingRequestsQuery, ErrorOr<List<SentPendingRequestResult>>>
{
    private readonly IPatientDoctorRequestRepository _requestRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly ICurrentUserService _currentUser;

    public GetSentPendingRequestsQueryHandler(
        IPatientDoctorRequestRepository requestRepository,
        IPatientRepository patientRepository,
        ICurrentUserService currentUser)
    {
        _requestRepository = requestRepository;
        _patientRepository = patientRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<List<SentPendingRequestResult>>> Handle(
        GetSentPendingRequestsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return AuthErrors.Forbidden;

        var callerPatientId = await _patientRepository.GetPatientIdByUserIdAsync(_currentUser.UserId.Value);
        if (callerPatientId != request.PatientId)
            return AuthErrors.Forbidden;

        var pendingRequests = await _requestRepository.GetPendingByPatientIdAsync(request.PatientId);

        var results = pendingRequests.Select(r => new SentPendingRequestResult(
            r.Id,
            r.DoctorId,
            r.Doctor.FirstName,
            r.Doctor.PaternalLastName,
            r.Doctor.Speciality,
            r.CreatedAt)).ToList();

        return results;
    }
}
