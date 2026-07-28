using Application.UseCases.DailyRecord.Common;
using Application.Common.Messaging;

namespace Application.UseCases.DailyRecord.Queries;

public sealed record GetDailyRecordByIdQuery(
    Guid PatientId,
    Guid RecordId) : IQuery<ErrorOr<DailyRecordResult>>;
