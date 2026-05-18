using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Common.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Admission> Admissions => Set<Admission>();
    public DbSet<DailyRecord> DailyRecords => Set<DailyRecord>();
    public DbSet<LabResult> LabResults => Set<LabResult>();
    public DbSet<ToolResult> ToolResults => Set<ToolResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}