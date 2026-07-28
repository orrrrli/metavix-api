using Application.Common.Exceptions;
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
    private readonly IUnitOfWork _unitOfWork =
        Substitute.For<IUnitOfWork>();

    private readonly RejectLinkRequestCommandHandler _handler;

    public RejectLinkRequestCommandHandlerTests()
    {
        _handler = new RejectLinkRequestCommandHandler(
            _requestRepository,
            _doctorRepository,
            _currentUser,
            _timeProvider,
            _unitOfWork);
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

        var linkRequest = TestEntities.LinkRequest(requestId, patientId, doctorId, RequestStatus.Pending);
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
        linkRequest.Status.Should().Be(RequestStatus.Rejected);
        linkRequest.ResolvedAt.Should().Be(now);
        _requestRepository.Received(1).MarkForUpdate(linkRequest);
        await _unitOfWork.Received(1).FlushAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNotPending_ReturnsNotPendingWithoutMutating()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        var linkRequest = TestEntities.LinkRequest(requestId, patientId, doctorId, RequestStatus.Accepted);
        var doctor = TestEntities.Doctor(doctorId, userId);

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns(doctor);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);

        // Act
        var result = await _handler.Handle(new RejectLinkRequestCommand(requestId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("LinkRequest.NotPending");
        _requestRepository.DidNotReceive().MarkForUpdate(Arg.Any<PatientDoctorRequest>());
        await _unitOfWork.DidNotReceive().FlushAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotTheDoctor_ReturnsForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        var linkRequest = TestEntities.LinkRequest(requestId, patientId, doctorId, RequestStatus.Pending);

        _currentUser.UserId.Returns(userId);
        // No doctor with this id belongs to userId → GetOwnedDoctorAsync returns null.
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns((Doctor?)null);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);

        // Act
        var result = await _handler.Handle(new RejectLinkRequestCommand(requestId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Auth.Forbidden");
        _requestRepository.DidNotReceive().MarkForUpdate(Arg.Any<PatientDoctorRequest>());
        await _unitOfWork.DidNotReceive().FlushAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenFlushThrowsConcurrencyConflict_ReturnsNotPending()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        var linkRequest = TestEntities.LinkRequest(requestId, patientId, doctorId, RequestStatus.Pending);
        var doctor = TestEntities.Doctor(doctorId, userId);

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns(doctor);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);
        _unitOfWork.FlushAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConcurrencyConflictException("conflict", new Exception()));

        // Act
        var result = await _handler.Handle(new RejectLinkRequestCommand(requestId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("LinkRequest.NotPending");
    }
}
