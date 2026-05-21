using Application.UseCases.InsulinDm1.Common;

namespace Application.UseCases.InsulinDm1.Queries;

public sealed record GetInsulinRecordByIdQuery(Guid PatientId, Guid RecordId) : IRequest<ErrorOr<InsulinDm1RecordResult>>;
