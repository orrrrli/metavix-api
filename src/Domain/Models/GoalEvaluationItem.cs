using Domain.Enums;

namespace Domain.Models;

public class GoalEvaluationItem
{
    public Guid Id { get; set; }
    public Guid GoalEvaluationId { get; set; }
    public string ParameterId { get; set; } = string.Empty;
    public decimal? ValueUsed { get; set; }

    // The InRange/AtRisk boundary the patient is compared against — null when the parameter was
    // not evaluated (NoData items): a non-existent reading cannot be "compared against" a goal.
    // Low-only specs (HDL, eGFR) used to surface a fallback band here on NoData; that was
    // misleading because GoalUsed is the goal, not the catalog threshold, and a NoData item
    // has no goal because it was not evaluated.
    public decimal? GoalUsed { get; set; }
    public GoalStatus Status { get; set; }

    // Populated for NoData items to explain why the parameter was not evaluated
    // (e.g. "not-evaluated-in-pregnancy", "requires-specialist-evaluation").
    public string? Reason { get; set; }

    public GoalEvaluation GoalEvaluation { get; set; } = null!;
}
