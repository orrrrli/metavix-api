using Application.UseCases.DailyRecord.Common;
using DomainDailyRecord = Domain.Models.DailyRecord;

namespace Application.UseCases.DailyRecord.Mappers;

internal static class BodyStatsMapper
{
    public static BodyStats ToResult(DomainDailyRecord? record) => new(
        record?.WeightKg,
        record?.WaistCm);
}
