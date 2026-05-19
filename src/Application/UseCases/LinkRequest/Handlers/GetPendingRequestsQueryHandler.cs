using Application.Common.Interfaces.Persistence;
using Application.UseCases.LinkRequest.Common;
using Application.UseCases.LinkRequest.Queries;

namespace Application.UseCases.LinkRequest.Handlers;

internal sealed class GetPendingRequestsQueryHandler
    : IRequestHandler<GetPendingRequestsQuery, ErrorOr<List<PendingRequestResult>>>
{
    private readonly IPatientDoctorRequestRepository _requestRepository;

    public GetPendingRequestsQueryHandler(IPatientDoctorRequestRepository requestRepository)
    {
        _requestRepository = requestRepository;
    }

    public async Task<ErrorOr<List<PendingRequestResult>>> Handle(
        GetPendingRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var pendingRequests = await _requestRepository.GetPendingByDoctorIdAsync(request.DoctorId);

        var results = pendingRequests.Select(r => new PendingRequestResult(
            r.Id,
            r.PatientId,
            r.Patient.FirstName,
            r.Patient.LastName,
            r.Patient.Email,
            r.CreatedAt)).ToList();

        return results;
    }
}
