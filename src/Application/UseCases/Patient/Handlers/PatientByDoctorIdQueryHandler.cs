using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.Patient.Common;
using Application.UseCases.Patient.Queries;
using MediatR;

namespace Application.UseCases.Patient.Handlers;

public class PatientByDoctorIdQueryHandler(
    IDoctorRepository doctorRepository,
    IPatientDoctorRequestRepository requestRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<PatientByDoctorIdQuery, ErrorOr<List<PatientResult>>>
{
    public async Task<ErrorOr<List<PatientResult>>> Handle(
        PatientByDoctorIdQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return AuthErrors.Forbidden;

        var callerDoctor = await doctorRepository.GetOwnedDoctorAsync(
            request.doctorId, currentUser.UserId.Value, cancellationToken);
        if (callerDoctor is null)
            return AuthErrors.Forbidden;

        var accepted = await requestRepository.GetAcceptedByDoctorIdAsync(request.doctorId);

        var result = accepted.Select(r => new PatientResult(
            r.Patient.Id,
            r.Patient.FirstName,
            r.Patient.LastName,
            r.Patient.MedicalRecordNumber)).ToList();

        return result;
    }
}