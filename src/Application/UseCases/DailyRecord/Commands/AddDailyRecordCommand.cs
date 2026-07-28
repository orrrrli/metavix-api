using Application.UseCases.DailyRecord.Common;
using Domain.Enums;
using Application.Common.Messaging;

namespace Application.UseCases.DailyRecord.Commands;

public sealed record GlucoseReading(
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
    List<GlucoseReading>? GlucoseReadings) : ITransactionalCommand<ErrorOr<DailyRecordResult>>;
