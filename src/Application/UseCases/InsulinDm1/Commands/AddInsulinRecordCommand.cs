using Application.UseCases.InsulinDm1.Common;
using Application.Common.Messaging;

namespace Application.UseCases.InsulinDm1.Commands;

public sealed record AddInsulinRecordCommand(
    Guid PatientId,
    DateOnly RecordDate,
    int? GlucoseBefore,
    int? GlucoseAfter,
    decimal? TotalCarbs,
    decimal? DoseApplied,
    string? MealDescription,
    string? HowIFelt) : ITransactionalCommand<ErrorOr<InsulinDm1RecordResult>>;
