namespace Application.UseCases.InsulinDm1.Common;

public sealed record InsulinDm1ProfileResult(
    Guid Id,
    Guid PatientId,
    string? InsulinName,
    decimal? Ric,
    int? SensitivityFactor,
    int? TargetGlucose,
    string? DoctorName,
    string? DoctorPhone,
    DateTime CreatedAt,
    DateTime UpdatedAt);
