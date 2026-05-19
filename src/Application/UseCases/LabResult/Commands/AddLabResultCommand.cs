using Application.UseCases.LabResult.Common;

namespace Application.UseCases.LabResult.Commands;

public sealed record AddLabResultCommand(
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
    string? Notes) : IRequest<ErrorOr<LabResultResult>>;
