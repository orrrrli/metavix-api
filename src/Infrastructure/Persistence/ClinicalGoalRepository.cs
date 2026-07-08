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

    public async Task<ClinicalGoal?> GetOwnedAsync(Guid goalId, Guid patientId, Guid doctorId)
    {
        return await _dbContext.ClinicalGoals.FirstOrDefaultAsync(
            g => g.Id == goalId && g.PatientId == patientId && g.DoctorId == doctorId);
    }

    public async Task AddAsync(ClinicalGoal goal)
    {
        await _dbContext.ClinicalGoals.AddAsync(goal);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(ClinicalGoal goal)
    {
        _dbContext.ClinicalGoals.Update(goal);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(ClinicalGoal goal)
    {
        _dbContext.ClinicalGoals.Remove(goal);
        await _dbContext.SaveChangesAsync();
    }
}
