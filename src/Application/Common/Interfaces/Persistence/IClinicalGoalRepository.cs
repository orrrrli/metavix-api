using Domain.Models;

namespace Application.Common.Interfaces.Persistence;

public interface IClinicalGoalRepository
{
    Task<List<ClinicalGoal>> GetByPatientIdAsync(Guid patientId);
    Task AddAsync(ClinicalGoal goal);
}
