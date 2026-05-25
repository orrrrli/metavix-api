using Application.UseCases.Patient.Common;

namespace Application.UseCases.Patient.Queries;

public sealed record GetPatientResumenQuery(
    Guid PatientId) : IRequest<ErrorOr<PatientResumenResult>>;
