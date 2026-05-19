namespace Domain.Models;

using Enums;

public class Patient
{
    public Guid Id { get; set; }
    public Guid? PrimaryDoctorId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public decimal? HeightCm { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DiabetesType DiabetesType { get; set; } = DiabetesType.None;
    public Guid? UserId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public Doctor? PrimaryDoctor { get; set; }
    public ICollection<Admission> Admissions { get; set; } = [];
    public ICollection<DailyRecord> DailyRecords { get; set; } = [];
    public ICollection<LabResult> LabResults { get; set; } = [];
    public ICollection<ToolResult> ToolResults { get; set; } = [];
}
