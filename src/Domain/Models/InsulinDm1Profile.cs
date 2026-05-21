namespace Domain.Models;

public class InsulinDm1Profile
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string? InsulinName { get; set; }
    public decimal? Ric { get; set; }
    public int? SensitivityFactor { get; set; }
    public int? TargetGlucose { get; set; }
    public string? DoctorName { get; set; }
    public string? DoctorPhone { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Patient Patient { get; set; } = null!;
}
