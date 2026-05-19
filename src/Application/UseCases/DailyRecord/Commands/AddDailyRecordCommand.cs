using Application.UseCases.DailyRecord.Common;

namespace Application.UseCases.DailyRecord.Commands;

public sealed record AddDailyRecordCommand(
    Guid PatientId,
    DateOnly RecordDate,
    TimeOnly? RecordTime,
    int? SystolicPressure,
    int? DiastolicPressure,
    int? HeartRate,
    decimal? WeightKg,
    int? WaistCm,
    string? Notes) : IRequest<ErrorOr<DailyRecordResult>>;
