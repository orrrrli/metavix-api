using Application.Common.Interfaces.Persistence;

namespace Infrastructure.Persistence;

public class GoalEvaluationRepository : IGoalEvaluationRepository
{
    private readonly AppDbContext _dbContext;

    public GoalEvaluationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(GoalEvaluation evaluation)
    {
        await _dbContext.GoalEvaluations.AddAsync(evaluation);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<GoalEvaluation?> GetLatestByPatientIdAsync(Guid patientId)
    {
        return await _dbContext.GoalEvaluations
            .Include(e => e.Items)
            .Where(e => e.PatientId == patientId)
            .OrderByDescending(e => e.EvaluatedAt)
            .FirstOrDefaultAsync();
    }
}
