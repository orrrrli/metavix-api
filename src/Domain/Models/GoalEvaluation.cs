using Domain.Enums;

namespace Domain.Models;

public class GoalEvaluation
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public EvaluationTrigger TriggeredBy { get; set; }
    public DateTime EvaluatedAt { get; set; }

    public Patient Patient { get; set; } = null!;
    public ICollection<GoalEvaluationItem> Items { get; set; } = [];
}
