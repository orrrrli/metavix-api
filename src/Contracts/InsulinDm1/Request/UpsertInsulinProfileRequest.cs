namespace Contracts.InsulinDm1.Request;

public record UpsertInsulinProfileRequest(
    string? InsulinName,
    decimal? Ric,
    int? SensitivityFactor,
    int? TargetGlucose,
    string? DoctorName,
    string? DoctorPhone);
