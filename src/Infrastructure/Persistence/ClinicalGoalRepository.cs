using Application.Common.Interfaces.Persistence;
using Domain.Models;

namespace Infrastructure.Persistence;

public class ClinicalGoalRepository : IClinicalGoalRepository
{
    private readonly AppDbContext _dbContext;

    public ClinicalGoalRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<List<ClinicalGoal>> GetByPatientIdAsync(Guid patientId)
    {
        return await _dbContext.ClinicalGoals
            .Where(g => g.PatientId == patientId)
            .ToListAsync();
    }

    public async Task<ClinicalGoal?> GetByIdAsync(Guid id)
    {
        return await _dbContext.ClinicalGoals.FirstOrDefaultAsync(g => g.Id == id);
    }

    // AsTracking: callers (Update/Delete handlers) mutate or remove the
    // returned goal and rely on PersistenceBehavior to commit — see
    // AppDbContext's global QueryTrackingBehavior.NoTracking default.
    public async Task<ClinicalGoal?> GetOwnedAsync(Guid goalId, Guid patientId, Guid doctorId)
    {
        return await _dbContext.ClinicalGoals.AsTracking().FirstOrDefaultAsync(
            g => g.Id == goalId && g.PatientId == patientId && g.DoctorId == doctorId);
    }

    public async Task AddAsync(ClinicalGoal goal)
    {
        await _dbContext.ClinicalGoals.AddAsync(goal);
    }

    // No-op body: goal is already tracked (loaded via GetByIdAsync/GetOwnedAsync,
    // no AsNoTracking), so EF's change tracker already recorded the mutated
    // properties. PersistenceBehavior commits via IUnitOfWork after the handler
    // returns success — see Application.Common.Behaviors.PersistenceBehavior.
    public Task UpdateAsync(ClinicalGoal goal) => Task.CompletedTask;

    public Task DeleteAsync(ClinicalGoal goal)
    {
        _dbContext.ClinicalGoals.Remove(goal);
        return Task.CompletedTask;
    }
}
