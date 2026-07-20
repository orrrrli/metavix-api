using Application.UseCases.DailyRecord.Common;
using Domain.Models;

namespace Application.Common.Mapping;

internal static class DailyRecordMapper
{
    public static DailyRecordResult ToResult(DailyRecord record) => new(
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
