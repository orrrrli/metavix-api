namespace Domain.Models;

public class ToolResult
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
}