using Domain.Enums;

namespace Application.UseCases.Goals.Common;

public sealed record EvaluateGoalsResult(
    Guid EvaluationId,
    DateTime EvaluatedAt,
    List<GoalEvaluationItemResult> Items);

public sealed record GoalEvaluationItemResult(
    string ParameterId,
    decimal? ValueUsed,
    decimal? ThresholdUsed,
    GoalStatus Status,
    string? Reason,
    string? CkdStage,
    // Derived flag: true when the evaluation used a doctor-set ClinicalGoal for this
    // parameter (custom thresholds merged onto the catalog spec via ApplyCustom), false
    // when only the ADA catalog spec was used. Lets the FE badge "Ajustada por tu doctor"
    // on the relevant ParametroMeta card without exposing the custom thresholds themselves.
    bool IsCustomGoal = false);
