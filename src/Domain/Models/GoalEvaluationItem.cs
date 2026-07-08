using Domain.Enums;

namespace Domain.Models;

public class GoalEvaluationItem
{
    public Guid Id { get; set; }
    public Guid GoalEvaluationId { get; set; }
    public string ParameterId { get; set; } = string.Empty;
    public decimal? ValueUsed { get; set; }
    public decimal GoalUsed { get; set; }
    public GoalStatus Status { get; set; }

    // Populated for NoData items to explain why the parameter was not evaluated
    // (e.g. "statins-contraindicated", "requires-specialist-evaluation").
    public string? Reason { get; set; }

    public GoalEvaluation GoalEvaluation { get; set; } = null!;
}
