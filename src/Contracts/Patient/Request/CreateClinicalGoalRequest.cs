namespace Contracts.Patient.Request;

public record CreateClinicalGoalRequest(
    string ParameterId,
    decimal? CustomOutOfRangeLow,
    decimal? CustomAtRiskLow,
    decimal? CustomAtRiskHigh,
    decimal? CustomOutOfRangeHigh);
