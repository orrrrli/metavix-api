using Domain.Models;

namespace Application.Common.Interfaces.Persistence;

public interface IGoalEvaluationRepository
{
    Task AddAsync(GoalEvaluation evaluation);
    Task<GoalEvaluation?> GetLatestByPatientIdAsync(Guid patientId);
}
