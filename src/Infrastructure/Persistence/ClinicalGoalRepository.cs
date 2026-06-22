using Application.Common.Interfaces.Persistence;

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

    public async Task AddAsync(ClinicalGoal goal)
    {
        await _dbContext.ClinicalGoals.AddAsync(goal);
        await _dbContext.SaveChangesAsync();
    }
}
