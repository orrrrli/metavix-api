namespace Application.UseCases.InsulinDm1.Common;

public sealed record InsulinDm1RecordResult(
    Guid Id,
    Guid PatientId,
    DateOnly RecordDate,
    int? GlucoseBefore,
    int? GlucoseAfter,
    decimal? TotalCarbs,
    decimal? DoseApplied,
    string? MealDescription,
    string? HowIFelt,
    DateTime CreatedAt);
