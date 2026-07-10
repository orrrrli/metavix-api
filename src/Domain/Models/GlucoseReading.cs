namespace Domain.Models;

using Enums;

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
}
