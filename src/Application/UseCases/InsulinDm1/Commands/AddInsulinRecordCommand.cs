using Application.UseCases.InsulinDm1.Common;

namespace Application.UseCases.InsulinDm1.Commands;

public sealed record AddInsulinRecordCommand(
    Guid PatientId,
    DateOnly RecordDate,
    int? GlucoseBefore,
    int? GlucoseAfter,
    decimal? TotalCarbs,
    decimal? DoseApplied,
    string? MealDescription,
    string? HowIFelt) : IRequest<ErrorOr<InsulinDm1RecordResult>>;
