using Domain.Models;

namespace Application.Common.Interfaces.Persistence;

public interface IClinicalGoalRepository
{
    Task<List<ClinicalGoal>> GetByPatientIdAsync(Guid patientId);
    Task<ClinicalGoal?> GetByIdAsync(Guid id);
    Task AddAsync(ClinicalGoal goal);
    Task UpdateAsync(ClinicalGoal goal);
    Task DeleteAsync(ClinicalGoal goal);
}
