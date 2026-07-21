using Application.UseCases.LabResult.Common;
using DomainLabResult = Domain.Models.LabResult;

namespace Application.UseCases.LabResult.Mappers;

internal static class LabResultMapper
{
    public static LabResultResult ToResult(DomainLabResult record) => new(
        record.Id,
        record.PatientId,
        record.SampleDate,
        record.Hba1c,
        record.TotalCholesterol,
        record.Ldl,
        record.Hdl,
        record.Triglycerides,
        record.Creatinine,
        record.Bun,
        record.EgoProteins,
        record.EgoGlucose,
        record.Notes,
        record.CreatedAt);
}
