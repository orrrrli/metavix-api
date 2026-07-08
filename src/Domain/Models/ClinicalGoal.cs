namespace Domain.Models;

public class ClinicalGoal
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public string ParameterId { get; set; } = string.Empty;

    // Doctor-set band boundaries. Any subset may be null; a null threshold falls back to the
    // catalog default (or, when no catalog spec applies, leaves that band open).
    public decimal? CustomOutOfRangeLow { get; set; }
    public decimal? CustomAtRiskLow { get; set; }
    public decimal? CustomAtRiskHigh { get; set; }
    public decimal? CustomOutOfRangeHigh { get; set; }

    public DateTime CreatedAt { get; set; }

    public Patient Patient { get; set; } = null!;
}
