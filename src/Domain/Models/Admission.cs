namespace Domain.Models;

public class Admission
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public DateTime AdmittedAt { get; set; }
    public DateTime? DischargedAt { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public bool IsActive => DischargedAt is null;

    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
}