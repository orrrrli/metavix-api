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
            .OrderBy(d => d.PaternalLastName)
            .ThenBy(d => d.FirstName)
            .ToListAsync();
    }

    public async Task<Doctor?> GetByIdAsync(Guid doctorId)
    {
        return await _dbContext.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId);
    }

    public async Task<Guid?> GetDoctorIdByUserIdAsync(Guid userId)
    {
        return await _dbContext.Doctors
            .Where(d => d.UserId == userId)
            .Select(d => (Guid?)d.Id)
            .FirstOrDefaultAsync();
    }

    public async Task UpdateVerificationAsync(
        Guid doctorId,
        bool isVerified,
        string? curp,
        string? ineNumber,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Doctors
            .Where(d => d.Id == doctorId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.IsVerified, isVerified)
                .SetProperty(d => d.Curp, curp)
                .SetProperty(d => d.IneNumber, ineNumber)
                .SetProperty(d => d.UpdatedAt, DateTime.UtcNow),
                cancellationToken);
    }
}