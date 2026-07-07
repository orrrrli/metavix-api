using Domain.Models;

namespace Application.Common.Interfaces.Persistence;

public interface INotificationRepository
{
    void Stage(Notification notification);
    Task<List<Notification>> GetByUserIdAsync(Guid userId);
    Task MarkReadAsync(Guid notificationId, Guid userId);
}
