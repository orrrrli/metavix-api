using Application.UseCases.DailyRecord.Common;

namespace Application.UseCases.DailyRecord.Queries;

public sealed record GetDailyRecordSnapshotQuery(
    Guid PatientId,
    DateOnly Date) : IRequest<ErrorOr<DailyRecordSnapshot>>;
