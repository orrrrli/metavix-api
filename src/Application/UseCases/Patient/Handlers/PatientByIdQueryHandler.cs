using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.Patient.Common;
using Application.UseCases.Patient.Queries;

namespace Application.UseCases.Patient.Handlers;

public class PatientByIdQueryHandler(
    IPatientRepository patientRepository,
    IDoctorRepository doctorRepository,
    IPatientDoctorRequestRepository requestRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<PatientByIdQuery, ErrorOr<PatientResult>>
{
    public async Task<ErrorOr<PatientResult>> Handle(
        PatientByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return AuthErrors.Forbidden;

        // The caller is identified by UserId; we don't have the route doctorId
        // in the query. Translate the caller to their doctorId and assert the
        // link exists with the requested patient.
        var callerDoctorId = await doctorRepository.GetDoctorIdByUserIdAsync(currentUser.UserId.Value);
        if (callerDoctorId is null)
            return AuthErrors.Forbidden;

        var isLinked = await requestRepository.IsAcceptedLinkAsync(callerDoctorId.Value, request.patientId, cancellationToken);
        if (!isLinked)
            return AuthErrors.Forbidden;

        PatientResult? result = await patientRepository.GetPatientByPatientId(request.patientId);
        return result == null ? (ErrorOr<PatientResult>)PatientErrors.PatientNotFound : (ErrorOr<PatientResult>)result;
    }
}