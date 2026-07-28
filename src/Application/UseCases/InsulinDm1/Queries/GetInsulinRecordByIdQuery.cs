using Application.UseCases.InsulinDm1.Common;
using Application.Common.Messaging;

namespace Application.UseCases.InsulinDm1.Queries;

public sealed record GetInsulinRecordByIdQuery(Guid PatientId, Guid RecordId) : IQuery<ErrorOr<InsulinDm1RecordResult>>;
