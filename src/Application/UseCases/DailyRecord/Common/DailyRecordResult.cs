namespace Application.UseCases.DailyRecord.Common;

public sealed record DailyRecordResult(
    Guid Id,
    Guid PatientId,
    DateOnly RecordDate,
    TimeOnly? RecordTime,
    int? SystolicPressure,
    int? DiastolicPressure,
    int? HeartRate,
    decimal? WeightKg,
    int? WaistCm,
    string? Notes,
    DateTime CreatedAt,
    List<GlucoseReadingResult> GlucoseReadings);
