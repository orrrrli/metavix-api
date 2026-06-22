namespace Domain.Models;

public class ClinicalGoal
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public string ParameterId { get; set; } = string.Empty;
    public decimal CustomValue { get; set; }
    public DateTime CreatedAt { get; set; }

    public Patient Patient { get; set; } = null!;
}
