namespace Domain.Models;

using Enums;

public class Patient
{
    public Guid Id { get; set; }
    public Guid? PrimaryDoctorId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MedicalRecordNumber { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public decimal? HeightCm { get; set; }
    public Gender? Gender { get; set; }
    public bool IsPregnant { get; set; } = false;
    public bool HasAscvd { get; set; } = false;
    public DateOnly? PregnancyStartDate { get; set; }
    public DateOnly? PregnancyDueDate { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DiabetesType DiabetesType { get; set; } = DiabetesType.None;
    public Guid UserId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public Doctor? PrimaryDoctor { get; set; }
    public ICollection<DailyRecord> DailyRecords { get; set; } = [];
    public ICollection<LabResult> LabResults { get; set; } = [];
    public ICollection<PatientDoctorRequest> LinkRequests { get; set; } = [];
    public InsulinDm1Profile? InsulinDm1Profile { get; set; }
    public ICollection<InsulinDm1Record> InsulinDm1Records { get; set; } = [];
}
