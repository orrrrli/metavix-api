using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.Patient.Common;
using Application.UseCases.Patient.Queries;
using MediatR;

namespace Application.UseCases.Patient.Handlers;

public class PatientByDoctorIdQueryHandler(
    IPatientRepository patientRepository,
    IDoctorRepository doctorRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<PatientByDoctorIdQuery, ErrorOr<List<PatientResult>>>
{
    public async Task<ErrorOr<List<PatientResult>>> Handle(
        PatientByDoctorIdQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return AuthErrors.Forbidden;

        var callerDoctorId = await doctorRepository.GetDoctorIdByUserIdAsync(currentUser.UserId.Value);
        if (callerDoctorId != request.doctorId)
            return AuthErrors.Forbidden;

        List<PatientResult> result = await patientRepository.GetAllPatientByDoctorId(request.doctorId);
        return result.Count == 0 ? (ErrorOr<List<PatientResult>>)PatientErrors.PatientsNotFound : (ErrorOr<List<PatientResult>>)result;
    }
}