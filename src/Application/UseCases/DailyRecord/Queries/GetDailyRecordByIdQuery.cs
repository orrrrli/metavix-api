using Application.UseCases.DailyRecord.Common;

namespace Application.UseCases.DailyRecord.Queries;

public sealed record GetDailyRecordByIdQuery(
    Guid RecordId) : IRequest<ErrorOr<DailyRecordResult>>;
