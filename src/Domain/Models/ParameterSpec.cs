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
    TimeSpan? NoDataWindow)
{
    // Bands are asymmetric: low side is exclusive (< OutOfRangeLow), high side is inclusive (>= OutOfRangeHigh).
    // A value at a boundary is reported in the worse band (AtRisk over InRange; OutOfRange over AtRisk).
    public GoalStatus Classify(decimal? value)
    {
        if (value is null)
            return GoalStatus.NoData;

        if (OutOfRangeLow.HasValue && value < OutOfRangeLow)
            return GoalStatus.OutOfRange;

        if (OutOfRangeHigh.HasValue && value >= OutOfRangeHigh)
            return GoalStatus.OutOfRange;

        if (AtRiskLow.HasValue && value < AtRiskLow)
            return GoalStatus.AtRisk;

        if (AtRiskHigh.HasValue && value >= AtRiskHigh)
            return GoalStatus.AtRisk;

        return GoalStatus.InRange;
    }
}
