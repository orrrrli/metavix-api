using Domain.Enums;

namespace Domain.Models;

public class Notification
{
    public Guid Id { get; set; }
    public Guid? RecipientUserId { get; set; }
    public Guid PatientId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; }

    public Patient? Patient { get; set; }
}
