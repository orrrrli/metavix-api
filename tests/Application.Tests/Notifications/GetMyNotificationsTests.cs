using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.Notifications.Queries;
using Domain.Enums;
using Domain.Models;

namespace Application.Tests.Notifications;

public class GetMyNotificationsTests
{
    private readonly INotificationRepository _notificationRepository = Substitute.For<INotificationRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private readonly GetMyNotificationsQueryHandler _handler;

    public GetMyNotificationsTests()
    {
        _handler = new GetMyNotificationsQueryHandler(_notificationRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_ReturnsNotificationsForCurrentUser()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _notificationRepository.GetByUserIdAsync(userId).Returns([
            new Notification
            {
                Id = Guid.NewGuid(),
                RecipientUserId = userId,
                PatientId = patientId,
                Title = "Paciente ahora en embarazo",
                Body = "María López fue marcada como embarazada el 2026-07-07.",
                Type = NotificationType.PregnancyActivated,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            }
        ]);

        var result = await _handler.Handle(new GetMyNotificationsQuery(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        result.Value[0].Type.Should().Be("PregnancyActivated");
        result.Value[0].IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NoUserId_ReturnsForbidden()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _handler.Handle(new GetMyNotificationsQuery(), CancellationToken.None);

        result.IsError.Should().BeTrue();
    }
}
