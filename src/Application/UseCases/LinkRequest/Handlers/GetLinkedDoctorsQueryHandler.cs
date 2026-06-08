using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.LinkRequest.Common;
using Application.UseCases.LinkRequest.Queries;

namespace Application.UseCases.LinkRequest.Handlers;

internal sealed class GetLinkedDoctorsQueryHandler
    : IRequestHandler<GetLinkedDoctorsQuery, ErrorOr<List<LinkedDoctorResult>>>
{
    private readonly IPatientDoctorRequestRepository _requestRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly ICurrentUserService _currentUser;

    public GetLinkedDoctorsQueryHandler(
        IPatientDoctorRequestRepository requestRepository,
        IPatientRepository patientRepository,
        ICurrentUserService currentUser)
    {
        _requestRepository = requestRepository;
        _patientRepository = patientRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<List<LinkedDoctorResult>>> Handle(
        GetLinkedDoctorsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return AuthErrors.Forbidden;

        var callerPatientId = await _patientRepository.GetPatientIdByUserIdAsync(_currentUser.UserId.Value);
        if (callerPatientId != request.PatientId)
            return AuthErrors.Forbidden;
        var acceptedRequests = await _requestRepository.GetAcceptedByPatientIdAsync(request.PatientId);

        var results = acceptedRequests.Select(r => new LinkedDoctorResult(
            r.Id,
            r.DoctorId,
            r.Doctor.FirstName,
            r.Doctor.PaternalLastName,
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
