using Application.UseCases.DailyRecord.Common;
using Application.Common.Messaging;

namespace Application.UseCases.DailyRecord.Queries;

public sealed record GetBodyStatsQuery(
    Guid PatientId,
    DateOnly Date) : IQuery<ErrorOr<BodyStats>>;
