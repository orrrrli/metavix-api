namespace Infrastructure.Persistence;

public class LogEntry
{
    public int Id { get; set; }
    public string Message { get; set; } = default!;
    public string MessageTemplate { get; set; } = default!;
    public string Level { get; set; } = default!;
    public DateTime RaiseDate { get; set; }
    public string? Exception { get; set; }
    public string? Properties { get; set; }
    public string? HttpMethod { get; set; }
    public string? Endpoint { get; set; }
    public string? CorrelationId { get; set; }
    public string? UserId { get; set; }
    public string? Role { get; set; }
}
