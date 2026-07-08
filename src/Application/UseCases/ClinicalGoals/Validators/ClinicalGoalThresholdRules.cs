using FluentValidation;

namespace Application.UseCases.ClinicalGoals.Validators;

// Shared coherence checks for the four custom thresholds, reused by the create and update validators.
internal static class ClinicalGoalThresholdRules
{
    public static void Validate<T>(
        ValidationContext<T> context,
        decimal? outOfRangeLow,
        decimal? atRiskLow,
        decimal? atRiskHigh,
        decimal? outOfRangeHigh)
    {
        if (outOfRangeLow is null && atRiskLow is null && atRiskHigh is null && outOfRangeHigh is null)
        {
            context.AddFailure("Debe definirse al menos uno de los cuatro umbrales.");
            return;
        }

        if (atRiskHigh is not null && outOfRangeHigh is null)
            context.AddFailure("Si se define atRiskHigh también debe definirse outOfRangeHigh.");

        if (atRiskLow is not null && outOfRangeLow is null)
            context.AddFailure("Si se define atRiskLow también debe definirse outOfRangeLow.");

        // Bands must widen monotonically: outOfRangeLow ≤ atRiskLow ≤ atRiskHigh ≤ outOfRangeHigh.
        if (outOfRangeLow is not null && atRiskLow is not null && outOfRangeLow > atRiskLow)
            context.AddFailure("Los umbrales no son coherentes: outOfRangeLow debe ser ≤ atRiskLow.");

        if (atRiskLow is not null && atRiskHigh is not null && atRiskLow > atRiskHigh)
            context.AddFailure("Los umbrales no son coherentes: atRiskLow debe ser ≤ atRiskHigh.");

        if (atRiskHigh is not null && outOfRangeHigh is not null && atRiskHigh > outOfRangeHigh)
            context.AddFailure("Los umbrales no son coherentes: atRiskHigh debe ser ≤ outOfRangeHigh.");
    }
}
