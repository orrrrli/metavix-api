namespace Contracts.InsulinDm1.Request;

public record AddInsulinRecordRequest(
    DateOnly RecordDate,
    int? GlucoseBefore,
    int? GlucoseAfter,
    decimal? TotalCarbs,
    decimal? DoseApplied,
    string? MealDescription,
    string? HowIFelt);
