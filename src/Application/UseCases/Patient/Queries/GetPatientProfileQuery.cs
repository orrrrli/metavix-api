using Application.UseCases.Patient.Common;

namespace Application.UseCases.Patient.Queries;

public sealed record GetPatientProfileQuery(Guid PatientId)
    : IRequest<ErrorOr<PatientProfileResult>>;
