using Application.UseCases.Patient.Common;
using Application.Common.Messaging;

namespace Application.UseCases.Patient.Queries;

public record PatientByIdQuery(
    Guid DoctorId,
    Guid PatientId) : IQuery<ErrorOr<PatientResult>>;
