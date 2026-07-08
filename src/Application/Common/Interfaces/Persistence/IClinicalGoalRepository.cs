using Domain.Models;

namespace Application.Common.Interfaces.Persistence;

public interface IClinicalGoalRepository
{
    Task<List<ClinicalGoal>> GetByPatientIdAsync(Guid patientId);
    Task<ClinicalGoal?> GetByIdAsync(Guid id);

    // Returns the goal only if it belongs to both the given patient and doctor; null otherwise.
    // Centralizes the ownership check so Update/Delete don't each re-derive it from GetByIdAsync.
    Task<ClinicalGoal?> GetOwnedAsync(Guid goalId, Guid patientId, Guid doctorId);

    Task AddAsync(ClinicalGoal goal);
    Task UpdateAsync(ClinicalGoal goal);
    Task DeleteAsync(ClinicalGoal goal);
}
