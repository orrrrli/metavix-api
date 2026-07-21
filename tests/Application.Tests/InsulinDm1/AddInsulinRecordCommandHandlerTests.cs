using Application.UseCases.InsulinDm1.Commands;
using Application.UseCases.InsulinDm1.Handlers;

namespace Application.Tests.InsulinDm1;

public class AddInsulinRecordCommandHandlerTests
{
    private readonly IInsulinDm1Repository _insulinRepository =
        Substitute.For<IInsulinDm1Repository>();
    private readonly IPatientRepository _patientRepository =
        Substitute.For<IPatientRepository>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();
    private readonly FakeTimeProvider _timeProvider = new();

    private readonly AddInsulinRecordCommandHandler _handler;

    public AddInsulinRecordCommandHandlerTests()
    {
        _handler = new AddInsulinRecordCommandHandler(
            _insulinRepository,
            _patientRepository,
            _currentUser,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_WhenPatientIsOwned_PersistsAndReturnsResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var patient = TestEntities.Patient(patientId);

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);
        _timeProvider.SetUtcNow(now);

        var command = new AddInsulinRecordCommand(
            patientId,
            new DateOnly(2026, 7, 20),
            120, 150, 45m, 5m, "Lunch", "Stable");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.PatientId.Should().Be(patientId);
        await _insulinRepository.Received(1).AddRecordAsync(
            Arg.Is<InsulinDm1Record>(r => r.PatientId == patientId && r.CreatedAt == now));
    }

    [Fact]
    public async Task Handle_WhenPatientIsNotOwned_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns((Patient?)null);

        var command = new AddInsulinRecordCommand(
            patientId,
            new DateOnly(2026, 7, 20),
            null, null, null, null, null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _insulinRepository.DidNotReceive().AddRecordAsync(Arg.Any<InsulinDm1Record>());
    }

    [Fact]
    public async Task Handle_WhenPatientIsInactive_ReturnsInactivePatient()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(TestEntities.Patient(patientId, isActive: false));

        var command = new AddInsulinRecordCommand(
            patientId,
            new DateOnly(2026, 7, 20),
            null, null, null, null, null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(RecordErrors.InactivePatient.Code);
        await _insulinRepository.DidNotReceive().AddRecordAsync(Arg.Any<InsulinDm1Record>());
    }

}
