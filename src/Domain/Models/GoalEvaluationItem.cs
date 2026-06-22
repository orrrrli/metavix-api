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

    public GoalEvaluation GoalEvaluation { get; set; } = null!;
}
