using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.UseCases.Patient.Common;
using Application.UseCases.Patient.Queries;

namespace Application.UseCases.Patient.Handlers;

public class PatientByIdQueryHandler (IPatientRepository patientRepository)
    : IRequestHandler<PatientByIdQuery, ErrorOr<PatientResult>>

{
    public async Task <ErrorOr<PatientResult>> Handle(
        PatientByIdQuery request, 
        CancellationToken cancellationToken)
    {
        Guid patientId = request.patientId;
        
        PatientResult? result = await patientRepository.GetPatientByPatientId(patientId);
        return result == null ? (ErrorOr<PatientResult>)PatientErrors.PatientNotFound : (ErrorOr<PatientResult>)result;
    }
    
}