using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.UseCases.LinkRequest.Common;
using Application.UseCases.LinkRequest.Queries;

namespace Application.UseCases.LinkRequest.Handlers;

internal sealed class GetLinkedDoctorsQueryHandler
    : IRequestHandler<GetLinkedDoctorsQuery, ErrorOr<List<LinkedDoctorResult>>>
{
    private readonly IPatientDoctorRequestRepository _requestRepository;

    public GetLinkedDoctorsQueryHandler(IPatientDoctorRequestRepository requestRepository)
    {
        _requestRepository = requestRepository;
    }

    public async Task<ErrorOr<List<LinkedDoctorResult>>> Handle(
        GetLinkedDoctorsQuery request,
        CancellationToken cancellationToken)
    {
        var acceptedRequests = await _requestRepository.GetAcceptedByPatientIdAsync(request.PatientId);

        var results = acceptedRequests.Select(r => new LinkedDoctorResult(
            r.Id,
            r.DoctorId,
            r.Doctor.FirstName,
            r.Doctor.LastName,
            r.Doctor.Speciality,
            r.Doctor.Email,
            r.ResolvedAt ?? r.CreatedAt)).ToList();

        if (results.Count == 0)
        {
            return LinkRequestErrors.RequestNotFound;
        }

        return results;
    }
}
