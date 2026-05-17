namespace Domain.Models;

using Enums;

public class DailyRecord
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid RecordedById { get; set; }
    public RecordSource Source { get; set; }
    public DateOnly RecordDate { get; set; }
    public TimeOnly? RecordTime { get; set; }
    public decimal? FastingGlucose { get; set; }
    public decimal? PostprandialGlucose { get; set; }
    public int? SystolicPressure { get; set; }
    public int? DiastolicPressure { get; set; }
    public int? HeartRate { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? WaistCm { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
}