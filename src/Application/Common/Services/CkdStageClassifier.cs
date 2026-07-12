using Application.Common.Constants;

namespace Application.Common.Services;

/// <summary>
/// Maps an eGFR value (mL/min/1.73m², CKD-EPI 2021) to a KDIGO 2024 chronic kidney
/// disease stage. Returns null when eGFR is null (no data) so the caller can leave
/// the <c>CkdStage</c> column empty and let the UI decide whether to render a
/// placeholder or hide the educational section.
///
/// Stages (KDIGO 2024 Clinical Practice Guideline for CKD, Table 1):
///   G1  ≥ 90          — Normal or high
///   G2  60–89         — Mildly decreased
///   G3a 45–59         — Mildly to moderately decreased
///   G3b 30–44         — Moderately to severely decreased
///   G4  15–29         — Severely decreased
///   G5  &lt; 15        — Kidney failure
///
/// Band boundaries match the AtRisk/OutOfRange thresholds in the eGFR
/// <see cref="ParameterSpec"/> (AdaGoalConstants.cs:97) so the chip's status
/// and the CKD stage stay consistent: InRange = G1-G2, AtRisk = G3a-G3b,
/// OutOfRange = G4-G5.
/// </summary>
public static class CkdStageClassifier
{
    public static string? Classify(decimal? egfr)
    {
        if (egfr is null) return null;

        // eGFR cannot be negative in clinical practice; treat as null to avoid
        // classifying garbage data as G5.
        if (egfr.Value < 0m) return null;

        return egfr.Value switch
        {
            >= 90m => AdaGoalConstants.CkdStageG1,
            >= 60m => AdaGoalConstants.CkdStageG2,
            >= 45m => AdaGoalConstants.CkdStageG3a,
            >= 30m => AdaGoalConstants.CkdStageG3b,
            >= 15m => AdaGoalConstants.CkdStageG4,
            _      => AdaGoalConstants.CkdStageG5,
        };
    }
}
