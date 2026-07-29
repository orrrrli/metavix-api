using Application.UseCases.Patient.Common;
using Application.Common.Messaging;

namespace Application.UseCases.Patient.Queries;

public sealed record GetMyPatientProfileQuery : IQuery<ErrorOr<PatientProfileResult>>;
