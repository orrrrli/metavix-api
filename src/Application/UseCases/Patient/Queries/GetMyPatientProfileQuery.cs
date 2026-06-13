using Application.UseCases.Patient.Common;

namespace Application.UseCases.Patient.Queries;

public sealed record GetMyPatientProfileQuery : IRequest<ErrorOr<PatientProfileResult>>;
