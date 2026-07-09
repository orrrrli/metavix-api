using Domain.Models;

namespace Application.UseCases.ClinicalGoals.Common;

internal static class ClinicalGoalMapper
{
    public static ClinicalGoalResult ToResult(ClinicalGoal goal) => new(
        goal.Id,
        goal.PatientId,
        goal.DoctorId,
        goal.ParameterId,
        goal.CustomOutOfRangeLow,
        goal.CustomAtRiskLow,
        goal.CustomAtRiskHigh,
        goal.CustomOutOfRangeHigh,
        goal.CreatedAt);
}
