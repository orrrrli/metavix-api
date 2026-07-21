using Application.UseCases.LabResult.Commands;
using Application.UseCases.LabResult.Handlers;

namespace Application.Tests.LabResults;

public class AddLabResultCommandHandlerTests
{
    private readonly ILabResultRepository _labResultRepository =
        Substitute.For<ILabResultRepository>();
    private readonly IPatientRepository _patientRepository =
        Substitute.For<IPatientRepository>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();
    private readonly FakeTimeProvider _timeProvider = new();

    private readonly AddLabResultCommandHandler _handler;

    public AddLabResultCommandHandlerTests()
    {
        _handler = new AddLabResultCommandHandler(
            _labResultRepository,
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
        var patient = BuildPatient(patientId);

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);
        _timeProvider.SetUtcNow(now);

        var command = new AddLabResultCommand(
            patientId,
            new DateOnly(2026, 7, 20),
            6.5m,
            180m,
            100m,
            50m,
            150m,
            0.9m,
            15m,
            "Negative",
            "Negative",
            "Routine");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.PatientId.Should().Be(patientId);
        result.Value.Hba1c.Should().Be(6.5m);
        await _labResultRepository.Received(1).AddAsync(
            Arg.Is<LabResult>(r => r.PatientId == patientId && r.CreatedAt == now));
    }

    [Fact]
    public async Task Handle_WhenPatientIsNotOwned_ReturnsForbidden()
    {
        // "Not found" and "not yours" both return null from the repository.
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns((Patient?)null);

        var command = new AddLabResultCommand(
            patientId,
            new DateOnly(2026, 7, 20),
            null, null, null, null, null, null, null, null, null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _labResultRepository.DidNotReceive().AddAsync(Arg.Any<LabResult>());
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsNull_ReturnsForbidden()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var command = new AddLabResultCommand(
            Guid.NewGuid(),
            new DateOnly(2026, 7, 20),
            null, null, null, null, null, null, null, null, null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _patientRepository.DidNotReceive()
            .GetOwnedPatientAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPatientIsInactive_ReturnsInactivePatient()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(BuildPatient(patientId, isActive: false));

        var command = new AddLabResultCommand(
            patientId,
            new DateOnly(2026, 7, 20),
            null, null, null, null, null, null, null, null, null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(RecordErrors.InactivePatient.Code);
        await _labResultRepository.DidNotReceive().AddAsync(Arg.Any<LabResult>());
    }

    private static Patient BuildPatient(Guid patientId, bool isActive = true) => new()
    {
        Id = patientId,
        UserId = Guid.NewGuid(),
        IsActive = isActive,
    };
}
