using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.UseCases.Patient.Common;
using Application.UseCases.Patient.Queries;
using MediatR;

namespace Application.UseCases.Patient.Handlers;

public class PatientByDoctorIdQueryHandler (IPatientRepository patientRepository) 
    : IRequestHandler<PatientByDoctorIdQuery, ErrorOr<List<PatientResult>>>
{
    public async Task <ErrorOr<List<PatientResult>>> Handle(
        PatientByDoctorIdQuery request, 
        CancellationToken cancellationToken)
    {
        Guid doctorId = request.doctorId;
        
        List<PatientResult> result = await patientRepository.GetAllPatientByDoctorId(doctorId);
        return result.Count == 0 ? (ErrorOr<List<PatientResult>>)PatientErrors.PatientsNotFound : (ErrorOr<List<PatientResult>>)result;
    }
}