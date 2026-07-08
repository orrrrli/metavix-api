using Domain.Models;
using FluentValidation;

namespace Application.UseCases.ClinicalGoals.Validators;

// Shared coherence checks for the four custom thresholds, reused by the create and update validators.
// The invariant itself lives in Domain.Models.ThresholdRange; this only maps issues to messages.
internal static class ClinicalGoalThresholdRules
{
    public static void Validate<T>(
        ValidationContext<T> context,
        decimal? outOfRangeLow,
        decimal? atRiskLow,
        decimal? atRiskHigh,
        decimal? outOfRangeHigh)
    {
        var range = new ThresholdRange(outOfRangeLow, atRiskLow, atRiskHigh, outOfRangeHigh);
        foreach (var issue in range.Validate())
            context.AddFailure(MessageFor(issue));
    }

    private static string MessageFor(ThresholdRangeIssue issue) => issue switch
    {
        ThresholdRangeIssue.NoThresholdsSet => "Debe definirse al menos uno de los cuatro umbrales.",
        ThresholdRangeIssue.PartialHighSide => "Si se define atRiskHigh también debe definirse outOfRangeHigh.",
        ThresholdRangeIssue.PartialLowSide => "Si se define atRiskLow también debe definirse outOfRangeLow.",
        ThresholdRangeIssue.LowBandIncoherent => "Los umbrales no son coherentes: outOfRangeLow debe ser ≤ atRiskLow.",
        ThresholdRangeIssue.MiddleBandIncoherent => "Los umbrales no son coherentes: atRiskLow debe ser ≤ atRiskHigh.",
        ThresholdRangeIssue.HighBandIncoherent => "Los umbrales no son coherentes: atRiskHigh debe ser ≤ outOfRangeHigh.",
        _ => throw new ArgumentOutOfRangeException(nameof(issue)),
    };
}
