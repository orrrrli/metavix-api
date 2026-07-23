namespace Contracts.LabResult.Request;

public record AddLabResultRequest(
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
    string? Notes);
