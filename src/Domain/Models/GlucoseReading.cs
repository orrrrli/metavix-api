namespace Domain.Models;

using Common.Errors;
using Enums;
using ErrorOr;

public class GlucoseReading
{
    public Guid Id { get; set; }
    public Guid DailyRecordId { get; set; }
    public GlucoseReadingType ReadingType { get; set; }
    public int ValueMgDl { get; set; }
    public TimeOnly? Time { get; set; }
    public string? Foods { get; set; }
    public PostprandialWindow? PostprandialWindow { get; set; }

    public DailyRecord DailyRecord { get; set; } = null!;

    // Factory: enforces clinical invariants for a glucose reading.
    // Primitives only — keeps Domain free of Application-layer types.
    // When a clinical concept gains its own rules (e.g. BloodPressure),
    // extract it into a ValueObject under Domain/ValueObjects/ instead of
    // widening this signature.
    public static ErrorOr<GlucoseReading> Create(
        Guid dailyRecordId,
        GlucoseReadingType type,
        int valueMgDl,
        TimeOnly? time,
        string? foods,
        PostprandialWindow? postprandialWindow,
        DateTime now)
    {
        if (valueMgDl <= 0 || valueMgDl > 600)
            return GlucoseReadingErrors.InvalidValue;

        // Per product decision (2026-07-20): every reading type requires Time.
        // The Time property on the entity stays nullable for forward-compat
        // with historical data, but the factory refuses new readings without it.
        if (time is null)
            return GlucoseReadingErrors.TimeRequired;

        var reading = new GlucoseReading
        {
            Id = Guid.NewGuid(),
            DailyRecordId = dailyRecordId,
            ReadingType = type,
            ValueMgDl = valueMgDl,
            Time = time,
            Foods = foods,
            PostprandialWindow = postprandialWindow
        };

        return reading;
    }
}
