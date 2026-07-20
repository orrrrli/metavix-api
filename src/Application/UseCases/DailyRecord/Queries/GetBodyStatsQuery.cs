using Application.UseCases.DailyRecord.Common;

namespace Application.UseCases.DailyRecord.Queries;

public sealed record GetBodyStatsQuery(
    Guid PatientId,
    DateOnly Date) : IRequest<ErrorOr<BodyStats>>;
