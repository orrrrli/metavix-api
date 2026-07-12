using Domain.Enums;

namespace Domain.Models;

public class GoalEvaluationItem
{
    public Guid Id { get; set; }
    public Guid GoalEvaluationId { get; set; }
    public string ParameterId { get; set; } = string.Empty;
    public decimal? ValueUsed { get; set; }

    // The InRange/AtRisk boundary the patient is compared against — the EFFECTIVE band after
    // MergeOnto has widened the catalog spec outward to keep it monotonic with the doctor's
    // custom goal. Renamed from GoalUsed because that name implied "the goal the doctor set",
    // which is misleading: a doctor's custom goal can be looser than the catalog spec, in which
    // case ThresholdUsed reflects the wider catalog band, not the typed-in custom. Null when the
    // parameter was not evaluated (NoData items): a non-existent reading cannot be compared
    // against a threshold.
    public decimal? ThresholdUsed { get; set; }
    public GoalStatus Status { get; set; }

    // Populated for NoData items to explain why the parameter was not evaluated
    // (e.g. "not-evaluated-in-pregnancy", "requires-specialist-evaluation").
    public string? Reason { get; set; }

    // KDIGO 2024 chronic kidney disease stage derived from ValueUsed when the
    // parameter is eGFR (e.g. "G1", "G3a", "G5"). Null for non-eGFR items and
    // for eGFR items without a numeric value. See CkdStageClassifier.
    public string? CkdStage { get; set; }

    // Persisted flag indicating the effective spec used to evaluate this item came from a
    // doctor-set ClinicalGoal (merged onto the catalog spec via ApplyCustom, or built from
    // custom alone in the pregnancy-no-catalog branch) rather than the ADA catalog spec alone.
    // Set by EvaluateGoalsCommandHandler.BuildItem in the same write that creates the
    // GoalEvaluation, so historical evaluations carry the flag too — the FE can render
    // 'Ajustada por tu doctor' for items evaluated under a custom goal even when reading past
    // results. See EvaluateGoalsResult.IsCustomGoal for the response contract.
    public bool IsCustomGoal { get; set; }

    public GoalEvaluation GoalEvaluation { get; set; } = null!;
}
