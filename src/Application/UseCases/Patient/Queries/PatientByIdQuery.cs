using Application.UseCases.Patient.Common;

namespace Application.UseCases.Patient.Queries;

public record PatientByIdQuery(
    Guid DoctorId,
    Guid patientId) : IRequest<ErrorOr<PatientResult>>;
