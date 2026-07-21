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
    public DbSet<DailyRecord> DailyRecords => Set<DailyRecord>();
    public DbSet<GlucoseReading> GlucoseReadings => Set<GlucoseReading>();
    public DbSet<LabResult> LabResults => Set<LabResult>();
    public DbSet<PatientDoctorRequest> PatientDoctorRequests => Set<PatientDoctorRequest>();
    public DbSet<LogEntry> Logs => Set<LogEntry>();
    public DbSet<InsulinDm1Profile> InsulinDm1Profiles => Set<InsulinDm1Profile>();
    public DbSet<InsulinDm1Record> InsulinDm1Records => Set<InsulinDm1Record>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<ClinicalGoal> ClinicalGoals => Set<ClinicalGoal>();
    public DbSet<GoalEvaluation> GoalEvaluations => Set<GoalEvaluation>();
    public DbSet<GoalEvaluationItem> GoalEvaluationItems => Set<GoalEvaluationItem>();
    public DbSet<Notification> Notifications => Set<Notification>();

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

        // Doctor configuration
        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.Property(d => d.Curp).HasMaxLength(18);
            entity.Property(d => d.IneNumber).HasMaxLength(18);
            entity.Property(d => d.IsVerified).HasDefaultValue(false);

            entity.HasOne(d => d.User)
                .WithOne(u => u.Doctor)
                .HasForeignKey<Doctor>(d => d.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        // User → Patient (one-to-one, required)
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasOne(p => p.User)
                .WithOne(u => u.Patient)
                .HasForeignKey<Patient>(p => p.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // Patient.MedicalRecordNumber is nullable (empty string
            // pre-acceptance is allowed) but, once assigned, must be globally
            // unique. A partial unique index excludes the empty-string
            // placeholder rows so they don't collide with each other. Column
            // type stays as `text` (unbounded) to match the pre-existing
            // schema — no length change in the migration.
            entity.HasIndex(p => p.MedicalRecordNumber)
                .IsUnique()
                .HasFilter("\"MedicalRecordNumber\" <> ''");
        });

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

        // InsulinDm1Profile — unique per patient
        modelBuilder.Entity<InsulinDm1Profile>(entity =>
        {
            entity.HasIndex(p => p.PatientId).IsUnique();
            entity.Property(p => p.InsulinName).HasMaxLength(80);
            entity.Property(p => p.DoctorName).HasMaxLength(120);
            entity.Property(p => p.DoctorPhone).HasMaxLength(30);
            entity.Property(p => p.Ric).HasPrecision(5, 1);

            entity.HasOne(p => p.Patient)
                .WithOne(p => p.InsulinDm1Profile)
                .HasForeignKey<InsulinDm1Profile>(p => p.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // InsulinDm1Record
        modelBuilder.Entity<InsulinDm1Record>(entity =>
        {
            entity.Property(r => r.TotalCarbs).HasPrecision(6, 1);
            entity.Property(r => r.DoseApplied).HasPrecision(5, 1);
            entity.Property(r => r.HowIFelt).HasMaxLength(200);

            entity.HasIndex(r => new { r.PatientId, r.RecordDate });

            entity.HasOne(r => r.Patient)
                .WithMany(p => p.InsulinDm1Records)
                .HasForeignKey(r => r.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RefreshToken configuration
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(r => r.Token).IsUnique();

            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PatientDoctorRequest configuration
        modelBuilder.Entity<PatientDoctorRequest>(entity =>
        {
            entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

            // Optimistic concurrency token. Two concurrent Accept/Reject/Revoke
            // calls both read Status=Pending in memory; without a row version
            // the second UpdateAsync would overwrite the first and run the
            // follow-up patient mutation twice. The token is a shadow `Version`
            // column (keeps the domain model free of persistence concerns).
            // PatientDoctorRequestRepository.UpdateAsync bumps it on every
            // write, so EF appends `WHERE "Version" = @original`; the loser of a
            // race matches zero rows and SaveChanges throws
            // DbUpdateConcurrencyException (→ repository returns false → handler
            // returns NotPending/NotAccepted).
            entity.Property<long>("Version")
                .IsConcurrencyToken()
                .HasDefaultValue(0L);

            entity.HasOne(r => r.Patient)
                .WithMany(p => p.LinkRequests)
                .HasForeignKey(r => r.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Doctor)
                .WithMany(d => d.LinkRequests)
                .HasForeignKey(r => r.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ClinicalGoal configuration
        modelBuilder.Entity<ClinicalGoal>(entity =>
        {
            entity.HasIndex(g => g.PatientId);
            entity.HasIndex(g => new { g.PatientId, g.ParameterId }).IsUnique();
            entity.Property(g => g.ParameterId).HasMaxLength(50).IsRequired();
            entity.Property(g => g.CustomOutOfRangeLow).HasPrecision(8, 2);
            entity.Property(g => g.CustomAtRiskLow).HasPrecision(8, 2);
            entity.Property(g => g.CustomAtRiskHigh).HasPrecision(8, 2);
            entity.Property(g => g.CustomOutOfRangeHigh).HasPrecision(8, 2);

            entity.HasOne(g => g.Patient)
                .WithMany()
                .HasForeignKey(g => g.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // GoalEvaluation configuration
        modelBuilder.Entity<GoalEvaluation>(entity =>
        {
            entity.HasIndex(e => e.PatientId);
            entity.Property(e => e.TriggeredBy).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(e => e.Patient)
                .WithMany()
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Items)
                .WithOne(i => i.GoalEvaluation)
                .HasForeignKey(i => i.GoalEvaluationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // GoalEvaluationItem configuration
        modelBuilder.Entity<GoalEvaluationItem>(entity =>
        {
            entity.HasIndex(i => i.GoalEvaluationId);
            entity.Property(i => i.ParameterId).HasMaxLength(50).IsRequired();
            entity.Property(i => i.ValueUsed).HasPrecision(10, 3);
            entity.Property(i => i.ThresholdUsed).HasPrecision(10, 3);
            entity.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(i => i.Reason).HasMaxLength(50);
            entity.Property(i => i.CkdStage).HasMaxLength(8);
            entity.Property(i => i.IsCustomGoal).IsRequired().HasDefaultValue(false);
        });

        // Notification configuration
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(n => n.RecipientUserId);
            entity.HasIndex(n => n.PatientId);
            entity.Property(n => n.Title).HasMaxLength(150).IsRequired();
            entity.Property(n => n.Body).HasMaxLength(500).IsRequired();
            entity.Property(n => n.Type).HasConversion<string>().HasMaxLength(40);

            entity.HasOne(n => n.Patient)
                .WithMany()
                .HasForeignKey(n => n.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
