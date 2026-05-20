using Domain.Models;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Common.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Admission> Admissions => Set<Admission>();
    public DbSet<DailyRecord> DailyRecords => Set<DailyRecord>();
    public DbSet<GlucoseReading> GlucoseReadings => Set<GlucoseReading>();
    public DbSet<LabResult> LabResults => Set<LabResult>();
    public DbSet<ToolResult> ToolResults => Set<ToolResult>();
    public DbSet<PatientDoctorRequest> PatientDoctorRequests => Set<PatientDoctorRequest>();
    public DbSet<LogEntry> Logs => Set<LogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();

            entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        });

        // User → Doctor (one-to-one, optional)
        modelBuilder.Entity<Doctor>()
            .HasOne(d => d.User)
            .WithOne(u => u.Doctor)
            .HasForeignKey<Doctor>(d => d.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // User → Patient (one-to-one, optional)
        modelBuilder.Entity<Patient>()
            .HasOne(p => p.User)
            .WithOne(u => u.Patient)
            .HasForeignKey<Patient>(p => p.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // LogEntry — table managed by Serilog, excluded from migrations
        modelBuilder.Entity<LogEntry>(entity =>
        {
            entity.ToTable("Logs", t => t.ExcludeFromMigrations());
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Id).HasColumnName("id");
            entity.Property(l => l.Message).HasColumnName("message");
            entity.Property(l => l.MessageTemplate).HasColumnName("message_template");
            entity.Property(l => l.Level).HasColumnName("level");
            entity.Property(l => l.RaiseDate).HasColumnName("raise_date");
            entity.Property(l => l.Exception).HasColumnName("exception");
            entity.Property(l => l.Properties).HasColumnName("properties");
            entity.Property(l => l.HttpMethod).HasColumnName("http_method");
            entity.Property(l => l.Endpoint).HasColumnName("endpoint");
            entity.Property(l => l.CorrelationId).HasColumnName("correlation_id");
            entity.Property(l => l.UserId).HasColumnName("user_id");
            entity.Property(l => l.Role).HasColumnName("role");
        });

        // PatientDoctorRequest configuration
        modelBuilder.Entity<PatientDoctorRequest>(entity =>
        {
            entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(r => r.Patient)
                .WithMany(p => p.LinkRequests)
                .HasForeignKey(r => r.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Doctor)
                .WithMany(d => d.LinkRequests)
                .HasForeignKey(r => r.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
