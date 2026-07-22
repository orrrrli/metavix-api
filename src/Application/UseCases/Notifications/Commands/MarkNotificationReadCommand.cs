using Application.Common.Authorization;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;

namespace Application.UseCases.Notifications.Commands;

public sealed record MarkNotificationReadCommand(Guid NotificationId) : IRequest<ErrorOr<Success>>;

internal sealed class MarkNotificationReadCommandHandler
    : IRequestHandler<MarkNotificationReadCommand, ErrorOr<Success>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUser;

    public MarkNotificationReadCommandHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUser)
    {
        _notificationRepository = notificationRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<Success>> Handle(
        MarkNotificationReadCommand request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserAccess.RequireUserId(_currentUser, out var userId) is { } userIdError)
            return userIdError;

        await _notificationRepository.MarkReadAsync(request.NotificationId, userId);

        return Result.Success;
    }
}
