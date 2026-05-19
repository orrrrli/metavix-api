using Application.Common.Interfaces.Persistence;
using Domain.Models;

namespace Infrastructure.Persistence;

public class DoctorRepository : IDoctorRepository
{
    private readonly AppDbContext _dbContext;

    public DoctorRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<List<Doctor>> GetAllActiveAsync()
    {
        return await _dbContext.Doctors
            .Where(d => d.IsActive)
            .OrderBy(d => d.LastName)
            .ThenBy(d => d.FirstName)
            .ToListAsync();
    }

    public async Task<Doctor?> GetByIdAsync(Guid doctorId)
    {
        return await _dbContext.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId);
    }
}