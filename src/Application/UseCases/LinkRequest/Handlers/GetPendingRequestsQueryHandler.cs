using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.LinkRequest.Common;
using Application.UseCases.LinkRequest.Queries;

namespace Application.UseCases.LinkRequest.Handlers;

internal sealed class GetPendingRequestsQueryHandler
    : IRequestHandler<GetPendingRequestsQuery, ErrorOr<List<PendingRequestResult>>>
{
    private readonly IPatientDoctorRequestRepository _requestRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly ICurrentUserService _currentUser;

    public GetPendingRequestsQueryHandler(
        IPatientDoctorRequestRepository requestRepository,
        IDoctorRepository doctorRepository,
        ICurrentUserService currentUser)
    {
        _requestRepository = requestRepository;
        _doctorRepository = doctorRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<List<PendingRequestResult>>> Handle(
        GetPendingRequestsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return AuthErrors.Forbidden;

        var callerDoctorId = await _doctorRepository.GetDoctorIdByUserIdAsync(_currentUser.UserId.Value);
        if (callerDoctorId != request.DoctorId)
            return AuthErrors.Forbidden;
        var pendingRequests = await _requestRepository.GetPendingByDoctorIdAsync(request.DoctorId);

        var results = pendingRequests.Select(r => new PendingRequestResult(
            r.Id,
            r.PatientId,
            r.Patient.FirstName,
            r.Patient.LastName,
            r.Patient.Email,
            r.CreatedAt)).ToList();

        if (results.Count == 0)
        {
            return LinkRequestErrors.RequestNotFound;
        }

        return results;
    }
}
