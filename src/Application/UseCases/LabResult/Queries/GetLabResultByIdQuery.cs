using Application.UseCases.LabResult.Common;

namespace Application.UseCases.LabResult.Queries;

public sealed record GetLabResultByIdQuery(
    Guid PatientId,
    Guid RecordId) : IRequest<ErrorOr<LabResultResult>>;
