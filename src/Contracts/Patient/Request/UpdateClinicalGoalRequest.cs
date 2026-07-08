namespace Contracts.Patient.Request;

public record UpdateClinicalGoalRequest(
    decimal? CustomOutOfRangeLow,
    decimal? CustomAtRiskLow,
    decimal? CustomAtRiskHigh,
    decimal? CustomOutOfRangeHigh);
