using Application.UseCases.InsulinDm1.Common;
using Domain.Models;

namespace Application.UseCases.InsulinDm1.Mappers;

internal static class InsulinDm1RecordMapper
{
    public static InsulinDm1RecordResult ToResult(InsulinDm1Record record) => new(
        record.Id,
        record.PatientId,
        record.RecordDate,
        record.GlucoseBefore,
        record.GlucoseAfter,
        record.TotalCarbs,
        record.DoseApplied,
        record.MealDescription,
        record.HowIFelt,
        record.CreatedAt);
}
