using Application.UseCases.DailyRecord.Common;
using Domain.Enums;

namespace Application.UseCases.DailyRecord.Commands;

public sealed record GlucoseReadingDto(
    GlucoseReadingType ReadingType,
    int ValueMgDl,
    TimeOnly? Time,
    string? Foods,
    PostprandialWindow? PostprandialWindow = null);

public sealed record AddDailyRecordCommand(
    Guid PatientId,
    DateOnly RecordDate,
    TimeOnly? RecordTime,
    int? SystolicPressure,
    int? DiastolicPressure,
    int? HeartRate,
    decimal? WeightKg,
    int? WaistCm,
    string? Notes,
    List<GlucoseReadingDto>? GlucoseReadings) : IRequest<ErrorOr<DailyRecordResult>>;
