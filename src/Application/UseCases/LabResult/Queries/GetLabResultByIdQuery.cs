using Application.UseCases.LabResult.Common;
using Application.Common.Messaging;

namespace Application.UseCases.LabResult.Queries;

public sealed record GetLabResultByIdQuery(
    Guid PatientId,
    Guid RecordId) : IQuery<ErrorOr<LabResultResult>>;
