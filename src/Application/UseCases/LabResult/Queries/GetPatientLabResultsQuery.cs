using Application.UseCases.LabResult.Common;

namespace Application.UseCases.LabResult.Queries;

public sealed record GetPatientLabResultsQuery(
    Guid PatientId) : IRequest<ErrorOr<List<LabResultResult>>>;
