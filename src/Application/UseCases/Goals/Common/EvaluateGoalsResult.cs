using Domain.Enums;

namespace Application.UseCases.Goals.Common;

public sealed record EvaluateGoalsResult(
    Guid EvaluationId,
    DateTime EvaluatedAt,
    List<GoalEvaluationItemResult> Items);

public sealed record GoalEvaluationItemResult(
    string ParameterId,
    decimal? ValueUsed,
    decimal GoalUsed,
    GoalStatus Status,
    string? Reason);
