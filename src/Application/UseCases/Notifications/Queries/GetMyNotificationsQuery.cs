using Application.Common.Errors;
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
        if (_currentUser.UserId is null)
            return AuthErrors.Forbidden;

        var notifications = await _notificationRepository.GetByUserIdAsync(_currentUser.UserId.Value);

        return notifications
            .Select(n => new NotificationResult(n.Id, n.Title, n.Body, n.Type.ToString(), n.IsRead, n.CreatedAt))
            .ToList();
    }
}
