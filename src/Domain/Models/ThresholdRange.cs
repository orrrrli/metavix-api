namespace Domain.Models;

public enum ThresholdRangeIssue
{
    NoThresholdsSet,
    PartialHighSide,
    PartialLowSide,
    LowBandIncoherent,
    MiddleBandIncoherent,
    HighBandIncoherent,
}

// Shared shape for the four-band (outOfRangeLow, atRiskLow, atRiskHigh, outOfRangeHigh)
// threshold model used by both ClinicalGoal (custom, partial) and ParameterSpec (catalog default,
// complete). Centralizes the monotonicity invariant so callers never construct an incoherent band
// set by hand.
public sealed record ThresholdRange(
    decimal? OutOfRangeLow,
    decimal? AtRiskLow,
    decimal? AtRiskHigh,
    decimal? OutOfRangeHigh)
{
    public bool IsEmpty =>
        OutOfRangeLow is null && AtRiskLow is null && AtRiskHigh is null && OutOfRangeHigh is null;

    // Strict coherence check for a range submitted on its own (e.g. a doctor's custom thresholds).
    public IEnumerable<ThresholdRangeIssue> Validate()
    {
        if (IsEmpty)
        {
            yield return ThresholdRangeIssue.NoThresholdsSet;
            yield break;
        }

        if (AtRiskHigh is not null && OutOfRangeHigh is null)
            yield return ThresholdRangeIssue.PartialHighSide;

        if (AtRiskLow is not null && OutOfRangeLow is null)
            yield return ThresholdRangeIssue.PartialLowSide;

        if (OutOfRangeLow is not null && AtRiskLow is not null && OutOfRangeLow > AtRiskLow)
            yield return ThresholdRangeIssue.LowBandIncoherent;

        if (AtRiskLow is not null && AtRiskHigh is not null && AtRiskLow > AtRiskHigh)
            yield return ThresholdRangeIssue.MiddleBandIncoherent;

        if (AtRiskHigh is not null && OutOfRangeHigh is not null && AtRiskHigh > OutOfRangeHigh)
            yield return ThresholdRangeIssue.HighBandIncoherent;
    }

    // Merges this range (typically a partial custom override) onto a set of defaults, widening
    // the neighboring bound outward whenever a value here would otherwise leave a monotonicity
    // gap against the default it's merged with. Guarantees the result satisfies
    // outOfRangeLow <= atRiskLow <= atRiskHigh <= outOfRangeHigh.
    public ThresholdRange MergeOnto(ThresholdRange defaults)
    {
        var outOfRangeLow = OutOfRangeLow ?? defaults.OutOfRangeLow;

        var atRiskLow = AtRiskLow ?? defaults.AtRiskLow;
        if (outOfRangeLow.HasValue && atRiskLow.HasValue && atRiskLow < outOfRangeLow)
            atRiskLow = outOfRangeLow;

        var atRiskHigh = AtRiskHigh ?? defaults.AtRiskHigh;
        if (atRiskLow.HasValue && atRiskHigh.HasValue && atRiskHigh < atRiskLow)
            atRiskHigh = atRiskLow;

        var outOfRangeHigh = OutOfRangeHigh ?? defaults.OutOfRangeHigh;
        if (atRiskHigh.HasValue && outOfRangeHigh.HasValue && outOfRangeHigh < atRiskHigh)
            outOfRangeHigh = atRiskHigh;

        return new ThresholdRange(outOfRangeLow, atRiskLow, atRiskHigh, outOfRangeHigh);
    }
}
