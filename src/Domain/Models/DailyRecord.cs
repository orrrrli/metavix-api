namespace Domain.Models;

public class DailyRecord
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public DateOnly RecordDate { get; set; }
    public TimeOnly? RecordTime { get; set; }
    public int? SystolicPressure { get; set; }
    public int? DiastolicPressure { get; set; }
    public int? HeartRate { get; set; }
    public decimal? WeightKg { get; set; }
    public int? WaistCm { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public Patient Patient { get; set; } = null!;
    public ICollection<GlucoseReading> GlucoseReadings { get; set; } = [];
}
