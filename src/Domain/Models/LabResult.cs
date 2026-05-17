namespace Domain.Models;

public class LabResult
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public DateOnly SampleDate { get; set; }
    public decimal? Hba1c { get; set; }
    public decimal? TotalCholesterol { get; set; }
    public decimal? Ldl { get; set; }
    public decimal? Hdl { get; set; }
    public decimal? Triglycerides { get; set; }
    public decimal? Creatinine { get; set; }
    public decimal? Bun { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
}