using Application.UseCases.Patient.Common;
using Application.Common.Messaging;

namespace Application.UseCases.Doctor.Queries;

public sealed record GetLinkedPatientProfileQuery(
    Guid DoctorId,
    Guid PatientId) : IQuery<ErrorOr<PatientProfileResult>>;
