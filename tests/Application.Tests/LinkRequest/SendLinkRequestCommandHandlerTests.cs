using Application.UseCases.LinkRequest.Commands;
using Application.UseCases.LinkRequest.Handlers;

namespace Application.Tests.LinkRequest;

public class SendLinkRequestCommandHandlerTests
{
    private readonly IPatientDoctorRequestRepository _requestRepository =
        Substitute.For<IPatientDoctorRequestRepository>();
    private readonly IDoctorRepository _doctorRepository =
        Substitute.For<IDoctorRepository>();
    private readonly IPatientRepository _patientRepository =
        Substitute.For<IPatientRepository>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();
    private readonly FakeTimeProvider _timeProvider = new();

    private readonly SendLinkRequestCommandHandler _handler;

    public SendLinkRequestCommandHandlerTests()
    {
        _handler = new SendLinkRequestCommandHandler(
            _requestRepository,
            _doctorRepository,
            _patientRepository,
            _currentUser,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_WhenPatientIsOwnedAndDoctorExists_CreatesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patient = TestEntities.Patient(patientId);
        var doctor = TestEntities.Doctor(doctorId);

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);
        _doctorRepository.GetByIdAsync(doctorId).Returns(doctor);
        _requestRepository.HasPendingRequestAsync(patientId, doctorId).Returns(false);

        var command = new SendLinkRequestCommand(patientId, doctorId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.PatientId.Should().Be(patientId);
        result.Value.DoctorId.Should().Be(doctorId);
        result.Value.Status.Should().Be("Pending");
        await _requestRepository.Received(1).AddAsync(
            Arg.Is<PatientDoctorRequest>(r => r.PatientId == patientId && r.DoctorId == doctorId));
    }

    [Fact]
    public async Task Handle_WhenPatientIsNotOwned_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns((Patient?)null);

        var command = new SendLinkRequestCommand(patientId, Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _doctorRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
        await _requestRepository.DidNotReceive().AddAsync(Arg.Any<PatientDoctorRequest>());
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsNull_ReturnsForbidden()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var command = new SendLinkRequestCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _patientRepository.DidNotReceive()
            .GetOwnedPatientAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

}
