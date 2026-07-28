using Application.UseCases.LabResult.Common;
using Application.Common.Messaging;

namespace Application.UseCases.Doctor.Queries;

public sealed record GetLinkedPatientLabResultsQuery(
    Guid DoctorId,
    Guid PatientId) : IQuery<ErrorOr<List<LabResultResult>>>;
