namespace Application.UseCases.LabResult.Common;

public sealed record LabResultResult(
    Guid Id,
    Guid PatientId,
    DateOnly SampleDate,
    decimal? Hba1c,
    decimal? TotalCholesterol,
    decimal? Ldl,
    decimal? Hdl,
    decimal? Triglycerides,
    decimal? Creatinine,
    decimal? Bun,
    string? EgoProteins,
    string? EgoGlucose,
    string? Notes,
    DateTime CreatedAt);
