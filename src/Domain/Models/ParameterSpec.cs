using Domain.Enums;

namespace Domain.Models;

public record ParameterSpec(
    string ParameterId,
    PatientCategory Category,
    Gender? Gender,
    decimal? OutOfRangeLow,
    decimal? AtRiskLow,
    decimal? AtRiskHigh,
    decimal? OutOfRangeHigh,
    bool AppliesInPregnancy,
    TimeSpan? NoDataWindow);
