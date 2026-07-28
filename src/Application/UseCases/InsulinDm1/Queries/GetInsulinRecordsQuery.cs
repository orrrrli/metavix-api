using Application.UseCases.InsulinDm1.Common;
using Application.Common.Messaging;

namespace Application.UseCases.InsulinDm1.Queries;

public sealed record GetInsulinRecordsQuery(Guid PatientId) : IQuery<ErrorOr<List<InsulinDm1RecordResult>>>;
