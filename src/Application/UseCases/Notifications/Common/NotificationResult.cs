namespace Application.UseCases.Notifications.Common;

public record NotificationResult(
    Guid Id,
    string Title,
    string Body,
    string Type,
    bool IsRead,
    DateTime CreatedAt);
