using Domain.Enums;

namespace Contracts.DailyRecord.Request;

public record AddDailyRecordRequest(
    DateOnly RecordDate,
    TimeOnly? RecordTime,
    int? SystolicPressure,
    int? DiastolicPressure,
    int? HeartRate,
    decimal? WeightKg,
    int? WaistCm,
    string? Notes,
    List<GlucoseReadingRequest>? GlucoseReadings);

public record GlucoseReadingRequest(
    GlucoseReadingType ReadingType,
    int ValueMgDl,
    TimeOnly? Time,
    string? Foods,
    PostprandialWindow? PostprandialWindow = null);
