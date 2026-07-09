namespace Application.UseCases.ClinicalGoals.Common;

public sealed record ClinicalGoalResult(
    Guid Id,
    Guid PatientId,
    Guid DoctorId,
    string ParameterId,
    decimal? CustomOutOfRangeLow,
    decimal? CustomAtRiskLow,
    decimal? CustomAtRiskHigh,
    decimal? CustomOutOfRangeHigh,
    DateTime CreatedAt);
