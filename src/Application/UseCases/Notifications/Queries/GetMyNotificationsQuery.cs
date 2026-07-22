using Application.Common.Authorization;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.Notifications.Common;

namespace Application.UseCases.Notifications.Queries;

public sealed record GetMyNotificationsQuery : IRequest<ErrorOr<List<NotificationResult>>>;

internal sealed class GetMyNotificationsQueryHandler
    : IRequestHandler<GetMyNotificationsQuery, ErrorOr<List<NotificationResult>>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUser;

    public GetMyNotificationsQueryHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUser)
    {
        _notificationRepository = notificationRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<List<NotificationResult>>> Handle(
        GetMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserAccess.RequireUserId(_currentUser, out var userId) is { } userIdError)
            return userIdError;

        var notifications = await _notificationRepository.GetByUserIdAsync(userId);

        return notifications
            .Select(n => new NotificationResult(n.Id, n.Title, n.Body, n.Type.ToString(), n.IsRead, n.CreatedAt))
            .ToList();
    }
}
