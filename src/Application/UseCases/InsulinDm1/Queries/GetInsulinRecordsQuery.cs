using Application.UseCases.InsulinDm1.Common;

namespace Application.UseCases.InsulinDm1.Queries;

public sealed record GetInsulinRecordsQuery(Guid PatientId) : IRequest<ErrorOr<List<InsulinDm1RecordResult>>>;
