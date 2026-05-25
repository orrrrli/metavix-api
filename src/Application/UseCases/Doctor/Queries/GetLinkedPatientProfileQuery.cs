using Application.UseCases.Patient.Common;

namespace Application.UseCases.Doctor.Queries;

public sealed record GetLinkedPatientProfileQuery(
    Guid DoctorId,
    Guid PatientId) : IRequest<ErrorOr<PatientProfileResult>>;
