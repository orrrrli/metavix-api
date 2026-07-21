using Application.UseCases.LinkRequest.Commands;
using Application.UseCases.LinkRequest.Handlers;
using Domain.Models;

namespace Application.Tests.LinkRequest;

public class RejectLinkRequestCommandHandlerTests
{
    private readonly IPatientDoctorRequestRepository _requestRepository =
        Substitute.For<IPatientDoctorRequestRepository>();
    private readonly IDoctorRepository _doctorRepository =
        Substitute.For<IDoctorRepository>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();
    private readonly FakeTimeProvider _timeProvider = new();

    private readonly RejectLinkRequestCommandHandler _handler;

    public RejectLinkRequestCommandHandlerTests()
    {
        _handler = new RejectLinkRequestCommandHandler(
            _requestRepository,
            _doctorRepository,
            _currentUser,
            _timeProvider);

        _requestRepository.UpdateAsync(Arg.Any<PatientDoctorRequest>()).Returns(true);
    }

    [Fact]
    public async Task Handle_WhenPending_TransitionsToRejected()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var linkRequest = new PatientDoctorRequest
        {
            Id = requestId,
            PatientId = patientId,
            DoctorId = doctorId,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };
        var doctor = TestEntities.Doctor(doctorId, userId);

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns(doctor);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);
        _timeProvider.SetUtcNow(now);

        // Act
        var result = await _handler.Handle(new RejectLinkRequestCommand(requestId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be("Rejected");
        await _requestRepository.Received(1).UpdateAsync(Arg.Is<PatientDoctorRequest>(r =>
            r.Status == RequestStatus.Rejected && r.ResolvedAt == now));
    }

    [Fact]
    public async Task Handle_WhenNotPending_ReturnsNotPendingWithoutMutating()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        var linkRequest = new PatientDoctorRequest
        {
            Id = requestId,
            PatientId = patientId,
            DoctorId = doctorId,
            Status = RequestStatus.Accepted,
            CreatedAt = DateTime.UtcNow,
        };
        var doctor = TestEntities.Doctor(doctorId, userId);

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns(doctor);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);

        // Act
        var result = await _handler.Handle(new RejectLinkRequestCommand(requestId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("LinkRequest.NotPending");
        await _requestRepository.DidNotReceive().UpdateAsync(Arg.Any<PatientDoctorRequest>());
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotTheDoctor_ReturnsForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        var linkRequest = new PatientDoctorRequest
        {
            Id = requestId,
            PatientId = patientId,
            DoctorId = doctorId,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };

        _currentUser.UserId.Returns(userId);
        // No doctor with this id belongs to userId → GetOwnedDoctorAsync returns null.
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns((Doctor?)null);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);

        // Act
        var result = await _handler.Handle(new RejectLinkRequestCommand(requestId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Auth.Forbidden");
        await _requestRepository.DidNotReceive().UpdateAsync(Arg.Any<PatientDoctorRequest>());
    }

}
