using Application.UseCases.DailyRecord.Common;
using Domain.Models;
using DomainDailyRecord = Domain.Models.DailyRecord;

namespace Application.UseCases.DailyRecord.Mappers;

internal static class DailyRecordMapper
{
    public static DailyRecordResult ToResult(DomainDailyRecord record) => new(
        record.Id,
        record.PatientId,
        record.RecordDate,
        record.RecordTime,
        record.SystolicPressure,
        record.DiastolicPressure,
        record.HeartRate,
        record.WeightKg,
        record.WaistCm,
        record.Notes,
        record.CreatedAt,
        record.GlucoseReadings
            .Select(g => new GlucoseReadingResult(
                g.Id, g.ReadingType, g.ValueMgDl, g.Time, g.Foods, g.PostprandialWindow))
            .ToList());
}
