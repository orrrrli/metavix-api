namespace Domain.Models;

public class InsulinDm1Record
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public DateOnly RecordDate { get; set; }
    public int? GlucoseBefore { get; set; }
    public int? GlucoseAfter { get; set; }
    public decimal? TotalCarbs { get; set; }
    public decimal? DoseApplied { get; set; }
    public string? MealDescription { get; set; }
    public string? HowIFelt { get; set; }
    public DateTime CreatedAt { get; set; }

    public Patient Patient { get; set; } = null!;
}
