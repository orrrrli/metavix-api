namespace Domain.Models;

public class Doctor
{
    public Guid Id { get; set; } 
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string Speciality { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public ICollection<Patient> Patients { get; set; } = [];
    public ICollection<Admission> Admissions { get; set; } = [];
    public ICollection<DailyRecord> DailyRecords { get; set; } = [];
    public ICollection<LabResult> LabResults { get; set; } = [];
    public ICollection<ToolResult> ToolResults { get; set; } = [];
}