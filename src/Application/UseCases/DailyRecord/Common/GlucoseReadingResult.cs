using Domain.Enums;

namespace Application.UseCases.DailyRecord.Common;

public sealed record GlucoseReadingResult(
    Guid Id,
    GlucoseReadingType ReadingType,
    int ValueMgDl,
    TimeOnly? Time,
    string? Foods);
