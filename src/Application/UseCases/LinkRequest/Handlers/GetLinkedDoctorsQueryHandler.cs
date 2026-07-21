using Application.Common.Authorization;
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
        // 1. Authenticate + load the owned patient (see PatientAccess).
        var access = await PatientAccess.RequireOwnedPatientAsync(
            _currentUser, _patientRepository, request.PatientId, cancellationToken);
        if (access.IsError)
            return access.Errors;

        var acceptedRequests = await _requestRepository.GetAcceptedByPatientIdAsync(request.PatientId);

        // 3. Map — a patient with no accepted links is a valid empty result,
        //    not an error. Returning RequestNotFound here would force callers
        //    to treat "no doctors yet" as a failure.
        return acceptedRequests.Select(r => new LinkedDoctorResult(
            r.Id,
            r.DoctorId,
            r.Doctor.FirstName,
            r.Doctor.PaternalLastName,
            r.Doctor.Speciality,
            r.Doctor.Email,
            r.ResolvedAt ?? r.CreatedAt)).ToList();
    }
}
