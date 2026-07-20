namespace Domain.Models;

using Common.Errors;
using ErrorOr;

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

    // Factory: enforces clinical invariants for a daily record.
    // Primitives only — keeps the signature flat and the entity
    // untouched for Code First. When BP, body measurements, or any
    // other pair of fields start carrying their own rules and
    // behavior, extract them into ValueObjects in Domain/ValueObjects/.
    public static ErrorOr<DailyRecord> Create(
        Guid patientId,
        DateOnly recordDate,
        TimeOnly? recordTime,
        int? systolicPressure,
        int? diastolicPressure,
        int? heartRate,
        decimal? weightKg,
        int? waistCm,
        string? notes,
        DateTime now,
        IReadOnlyList<GlucoseReading>? glucoseReadings = null)
    {
        // Clinical invariant: blood pressure must come as a pair.
        if (systolicPressure.HasValue != diastolicPressure.HasValue)
            return DailyRecordErrors.IncompleteBloodPressure;

        var record = new DailyRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            RecordDate = recordDate,
            RecordTime = recordTime,
            SystolicPressure = systolicPressure,
            DiastolicPressure = diastolicPressure,
            HeartRate = heartRate,
            WeightKg = weightKg,
            WaistCm = waistCm,
            Notes = notes,
            CreatedAt = now,
            GlucoseReadings = glucoseReadings?.ToList() ?? []
        };

        return record;
    }
}
