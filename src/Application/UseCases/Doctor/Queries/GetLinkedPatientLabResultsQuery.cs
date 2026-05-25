using Application.UseCases.LabResult.Common;

namespace Application.UseCases.Doctor.Queries;

public sealed record GetLinkedPatientLabResultsQuery(
    Guid DoctorId,
    Guid PatientId) : IRequest<ErrorOr<List<LabResultResult>>>;
