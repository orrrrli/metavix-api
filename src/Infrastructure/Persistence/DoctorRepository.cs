using Application.Common.Interfaces.Persistence;
using Domain.Models;

namespace Infrastructure.Persistence;

public class DoctorRepository : IDoctorRepository
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public DoctorRepository(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
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

    public async Task<Doctor?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);
    }

    // Returns the Doctor only if it exists AND belongs to userId.
    // Collapses "not found" and "not yours" into a single null so the
    // handler can't leak which doctor IDs exist (enumeration oracle).
    public async Task<Doctor?> GetOwnedDoctorAsync(
        Guid doctorId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(
                d => d.Id == doctorId && d.UserId == userId,
                cancellationToken);
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
                .SetProperty(d => d.UpdatedAt, _timeProvider.GetUtcNow().UtcDateTime),
                cancellationToken);
    }

    public async Task UpdateProfileAsync(
        Guid doctorId,
        string licenseNumber,
        string speciality,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Doctors
            .Where(d => d.Id == doctorId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.LicenseNumber, licenseNumber)
                .SetProperty(d => d.Speciality, speciality)
                .SetProperty(d => d.UpdatedAt, _timeProvider.GetUtcNow().UtcDateTime),
                cancellationToken);
    }
}