using Application.Common.Interfaces.Persistence;
using Domain.Enums;
using Domain.Models;

namespace Infrastructure.Persistence;

public class PatientDoctorRequestRepository : IPatientDoctorRequestRepository
{
    private readonly AppDbContext _dbContext;

    public PatientDoctorRequestRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(PatientDoctorRequest request)
    {
        await _dbContext.PatientDoctorRequests.AddAsync(request);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<PatientDoctorRequest?> GetByIdAsync(Guid id)
    {
        return await _dbContext.PatientDoctorRequests
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<PatientDoctorRequest>> GetPendingByDoctorIdAsync(Guid doctorId)
    {
        return await _dbContext.PatientDoctorRequests
            .Include(r => r.Patient)
            .Where(r => r.DoctorId == doctorId && r.Status == RequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> HasPendingRequestAsync(Guid patientId, Guid doctorId)
    {
        return await _dbContext.PatientDoctorRequests
            .AnyAsync(r => r.PatientId == patientId
                        && r.DoctorId == doctorId
                        && r.Status == RequestStatus.Pending);
    }

    public async Task UpdateAsync(PatientDoctorRequest request)
    {
        _dbContext.PatientDoctorRequests.Update(request);
        await _dbContext.SaveChangesAsync();
    }
}
